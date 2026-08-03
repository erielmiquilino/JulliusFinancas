import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { finalize } from 'rxjs/operators';
import { CanComponentDeactivate } from '../../../cards/guards/unsaved-changes.guard';
import { Category, CategoryService } from '../../../categories/services/category.service';
import { ConfirmActionDialogComponent } from '../confirm-action-dialog/confirm-action-dialog.component';
import { extractApiError } from '../../services/api-error';
import {
  ReconciliationItem,
  ReconciliationItemStatus,
  ReconciliationReviewFlag,
  ReconciliationService,
  ReconciliationSession,
  TransactionType
} from '../../services/reconciliation.service';

interface RowEdit {
  description: string;
  categoryId: string | null;
}

@Component({
  selector: 'app-reconciliation-review',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSnackBarModule,
    MatTooltipModule,
    CurrencyPipe,
    DatePipe
  ],
  templateUrl: './reconciliation-review.component.html',
  styleUrls: ['./reconciliation-review.component.scss']
})
export class ReconciliationReviewComponent implements OnInit, CanComponentDeactivate {
  private readonly service = inject(ReconciliationService);
  private readonly categoryService = inject(CategoryService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly ItemStatus = ReconciliationItemStatus;
  readonly ReviewFlag = ReconciliationReviewFlag;
  readonly TransactionType = TransactionType;

  readonly session = signal<ReconciliationSession | null>(null);
  readonly categories = signal<Category[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly busyItemId = signal<string | null>(null);

  /** Edições ainda não persistidas, por linha. */
  private readonly edits = new Map<string, RowEdit>();
  readonly dirtyCount = signal(0);

  readonly needsAttention = computed(() =>
    (this.session()?.items ?? []).filter(
      item =>
        item.reviewFlag !== ReconciliationReviewFlag.None &&
        item.status !== ReconciliationItemStatus.Ignored &&
        item.status !== ReconciliationItemStatus.NettedInternal
    )
  );

  readonly ready = computed(() =>
    (this.session()?.items ?? []).filter(
      item =>
        item.reviewFlag === ReconciliationReviewFlag.None &&
        (item.status === ReconciliationItemStatus.Pending ||
          item.status === ReconciliationItemStatus.Approved)
    )
  );

  readonly netted = computed(() =>
    (this.session()?.items ?? []).filter(
      item => item.status === ReconciliationItemStatus.NettedInternal
    )
  );

  readonly ignored = computed(() =>
    (this.session()?.items ?? []).filter(
      item => item.status === ReconciliationItemStatus.Ignored
    )
  );

  readonly canConfirm = computed(() => this.needsAttention().length === 0 && this.ready().length > 0);

  readonly balanceMatches = computed(() => {
    const current = this.session();
    return current ? Math.abs(current.projectedBalance - current.bankBalance) < 0.005 : false;
  });

  ngOnInit(): void {
    this.categoryService.getAllCategories().subscribe({
      next: categories => this.categories.set(categories),
      error: error => this.showError('Erro ao carregar categorias', error)
    });

    const sessionId = this.route.snapshot.paramMap.get('id');
    if (sessionId) {
      this.loadSession(sessionId);
    } else {
      this.loadOpenSession();
    }
  }

  canDeactivate(): boolean {
    if (this.dirtyCount() === 0) {
      return true;
    }

    return confirm('Há edições não salvas nesta conciliação. Deseja sair mesmo assim?');
  }

  loadSession(id: string): void {
    this.loading.set(true);
    this.service
      .getSession(id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: session => this.applySession(session),
        error: error => this.showError('Erro ao carregar conciliação', error)
      });
  }

  loadOpenSession(): void {
    this.loading.set(true);
    this.service
      .getOpenSession()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: session => {
          if (session) {
            this.applySession(session);
          }
        },
        error: error => this.showError('Erro ao carregar conciliação', error)
      });
  }

  descriptionOf(item: ReconciliationItem): string {
    return this.edits.get(item.id)?.description ?? item.proposedDescription;
  }

  categoryOf(item: ReconciliationItem): string | null {
    const edit = this.edits.get(item.id);
    return edit ? edit.categoryId : item.proposedCategoryId;
  }

  onDescriptionChange(item: ReconciliationItem, value: string): void {
    this.trackEdit(item, { description: value, categoryId: this.categoryOf(item) });
  }

  onCategoryChange(item: ReconciliationItem, categoryId: string): void {
    this.trackEdit(item, { description: this.descriptionOf(item), categoryId });
  }

  approve(item: ReconciliationItem): void {
    const categoryId = this.categoryOf(item);
    if (!categoryId) {
      this.snackBar.open('Escolha uma categoria antes de aprovar.', 'Fechar', { duration: 4000 });
      return;
    }

    this.persist(item, ReconciliationItemStatus.Approved);
  }

  ignore(item: ReconciliationItem): void {
    this.persist(item, ReconciliationItemStatus.Ignored);
  }

  /** Anula manualmente uma transferência interna cujo par não apareceu no período. */
  netManually(item: ReconciliationItem): void {
    this.persist(item, ReconciliationItemStatus.NettedInternal);
  }

  /** Traz de volta uma linha marcada como transferência interna para ser lançada normalmente. */
  restore(item: ReconciliationItem): void {
    this.persist(item, ReconciliationItemStatus.Pending);
  }

  confirm(): void {
    const current = this.session();
    if (!current) {
      return;
    }

    const dialogRef = this.dialog.open(ConfirmActionDialogComponent, {
      width: '480px',
      data: {
        title: 'Confirmar conciliação',
        message: `${this.ready().length} lançamento(s) serão gravados no Jullius.`,
        details: [
          { label: 'Entradas', value: this.formatCurrency(current.totalIncome) },
          { label: 'Saídas', value: this.formatCurrency(current.totalExpenses) },
          { label: 'Saldo projetado', value: this.formatCurrency(current.projectedBalance) },
          { label: 'Saldo nos bancos', value: this.formatCurrency(current.bankBalance) }
        ],
        confirmLabel: 'Confirmar e lançar',
        confirmColor: 'primary',
        confirmIcon: 'done_all',
        warningMessage: this.balanceMatches()
          ? undefined
          : 'O saldo projetado não bate com a soma das contas. Confira antes de confirmar.'
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) {
        return;
      }

      this.saving.set(true);
      this.service
        .confirmSession(current.id)
        .pipe(finalize(() => this.saving.set(false)))
        .subscribe({
          next: result => {
            this.edits.clear();
            this.dirtyCount.set(0);
            this.snackBar.open(
              `${result.postedCount} lançamento(s) gravados. Em Conta: ${this.formatCurrency(result.emConta)} ` +
                `(divergência ${this.formatCurrency(result.divergencia)}).`,
              'Fechar',
              { duration: 10000, panelClass: 'success-snackbar' }
            );
            this.router.navigate(['/dashboard']);
          },
          error: error => this.showError('Erro ao confirmar conciliação', error)
        });
    });
  }

  discard(): void {
    const current = this.session();
    if (!current) {
      return;
    }

    const dialogRef = this.dialog.open(ConfirmActionDialogComponent, {
      width: '450px',
      data: {
        title: 'Descartar conciliação',
        message: 'Todos os lançamentos desta revisão serão descartados e nada será gravado.',
        confirmLabel: 'Descartar',
        confirmColor: 'warn',
        confirmIcon: 'delete_sweep',
        warningMessage: 'Eles poderão ser trazidos de novo numa sincronização futura.'
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) {
        return;
      }

      this.service.discardSession(current.id).subscribe({
        next: () => {
          this.edits.clear();
          this.dirtyCount.set(0);
          this.snackBar.open('Conciliação descartada.', 'Fechar', { duration: 4000 });
          this.router.navigate(['/reconciliation']);
        },
        error: error => this.showError('Erro ao descartar conciliação', error)
      });
    });
  }

  flagLabel(flag: ReconciliationReviewFlag): string {
    switch (flag) {
      case ReconciliationReviewFlag.AmbiguousCategory:
        return 'Categoria indefinida';
      case ReconciliationReviewFlag.OrphanTransfer:
        return 'Transferência sem par';
      case ReconciliationReviewFlag.PossibleDuplicate:
        return 'Possível duplicata';
      default:
        return '';
    }
  }

  private applySession(session: ReconciliationSession): void {
    this.session.set(session);
    this.edits.clear();
    this.dirtyCount.set(0);
    session.warnings.forEach(warning => this.snackBar.open(warning, 'Fechar', { duration: 8000 }));
  }

  private trackEdit(item: ReconciliationItem, edit: RowEdit): void {
    const unchanged =
      edit.description === item.proposedDescription && edit.categoryId === item.proposedCategoryId;

    if (unchanged) {
      this.edits.delete(item.id);
    } else {
      this.edits.set(item.id, edit);
    }

    this.dirtyCount.set(this.edits.size);
  }

  private persist(item: ReconciliationItem, status: ReconciliationItemStatus): void {
    this.busyItemId.set(item.id);

    this.service
      .updateItem(item.id, {
        description: this.descriptionOf(item),
        categoryId: this.categoryOf(item),
        status
      })
      .pipe(finalize(() => this.busyItemId.set(null)))
      .subscribe({
        next: updated => {
          this.edits.delete(item.id);
          this.dirtyCount.set(this.edits.size);
          this.replaceItem(updated);
        },
        error: error => this.showError('Erro ao salvar revisão', error)
      });
  }

  /**
   * Recarrega a sessão para os totais e o saldo projetado do rodapé
   * refletirem a linha recém-alterada.
   */
  private replaceItem(updated: ReconciliationItem): void {
    const current = this.session();
    if (!current) {
      return;
    }

    this.session.set({
      ...current,
      items: current.items.map(item => (item.id === updated.id ? updated : item))
    });

    this.service.getSession(current.id).subscribe({
      next: session =>
        this.session.set({
          ...session,
          items: session.items
        }),
      error: () => void 0
    });
  }

  private formatCurrency(value: number): string {
    return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  private showError(prefix: string, error: unknown): void {
    this.snackBar.open(`${prefix}: ${extractApiError(error)}`, 'Fechar', {
      duration: 8000,
      panelClass: 'error-snackbar'
    });
  }
}
