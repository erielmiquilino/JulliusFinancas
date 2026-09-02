import { ChangeDetectionStrategy, Component, Inject, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatRadioModule } from '@angular/material/radio';
import { debounceTime, distinctUntilChanged, finalize, Subject, switchMap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  LinkItemRequest,
  MatchCandidate,
  ReconciliationItem,
  ReconciliationService
} from '../../services/reconciliation.service';

export interface LinkTransactionDialogData {
  item: ReconciliationItem;
}

/**
 * Aponta uma linha do extrato para um lançamento que já existe no Jullius, em vez de criar outro.
 *
 * Cobre os três casos reais: o gasto já lançado e pago (vincula sem alterar nada), a projeção com
 * valor estimado que o pagamento corrigiu, e várias cobranças do banco para uma única parcela.
 */
@Component({
  selector: 'app-link-transaction-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule, MatDialogModule, MatButtonModule, MatCheckboxModule,
    MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule,
    MatRadioModule, CurrencyPipe, DatePipe
  ],
  templateUrl: './link-transaction-dialog.component.html',
  styleUrls: ['./link-transaction-dialog.component.scss']
})
export class LinkTransactionDialogComponent implements OnInit {
  private readonly service = inject(ReconciliationService);
  private readonly searchInput = new Subject<string>();

  readonly candidates = signal<MatchCandidate[]>([]);
  readonly loading = signal(false);
  readonly selectedId = signal<string | null>(null);

  searchTerm = '';
  updateAmount = false;
  updateDueDate = false;
  markAsPaid = false;

  readonly selected = computed(() =>
    this.candidates().find(c => c.transactionId === this.selectedId()) ?? null
  );

  /** Acima deste score o candidato é apresentado como sugestão destacada. */
  private static readonly STRONG = 0.8;

  constructor(
    private dialogRef: MatDialogRef<LinkTransactionDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: LinkTransactionDialogData
  ) {
    this.searchInput
      .pipe(
        debounceTime(350),
        distinctUntilChanged(),
        switchMap(term => {
          this.loading.set(true);
          return this.service
            .getMatchCandidates(this.data.item.id, term || undefined)
            .pipe(finalize(() => this.loading.set(false)));
        }),
        takeUntilDestroyed()
      )
      .subscribe({
        next: candidates => this.candidates.set(candidates),
        error: () => this.candidates.set([])
      });
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service
      .getMatchCandidates(this.data.item.id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: candidates => {
          this.candidates.set(candidates);
          // Pré-seleciona o melhor candidato só quando ele é forte, para o caso comum
          // ficar a um clique — mas nunca vincula sozinho.
          const best = candidates[0];
          if (best && best.score >= LinkTransactionDialogComponent.STRONG) {
            this.select(best);
          }
        },
        error: () => this.candidates.set([])
      });
  }

  onSearch(term: string): void {
    this.searchInput.next(term);
  }

  select(candidate: MatchCandidate): void {
    this.selectedId.set(candidate.transactionId);
    // As correções vêm marcadas conforme o que de fato diverge.
    this.updateAmount = candidate.suggestUpdateAmount;
    this.updateDueDate = candidate.suggestUpdateDueDate;
    this.markAsPaid = candidate.suggestMarkAsPaid;
  }

  isStrong(candidate: MatchCandidate): boolean {
    return candidate.score >= LinkTransactionDialogComponent.STRONG;
  }

  /** Valor que o lançamento passará a ter: a soma, quando outras linhas já apontam para ele. */
  targetAmount(candidate: MatchCandidate): number {
    return candidate.alreadyLinkedAmount > 0 ? candidate.combinedAmount : this.data.item.absoluteAmount;
  }

  onCancel(): void {
    this.dialogRef.close(null);
  }

  onConfirm(): void {
    const candidate = this.selected();
    if (!candidate) {
      return;
    }

    const request: LinkItemRequest = {
      transactionId: candidate.transactionId,
      updateAmount: this.updateAmount,
      updateDueDate: this.updateDueDate,
      markAsPaid: this.markAsPaid
    };

    this.dialogRef.close(request);
  }
}
