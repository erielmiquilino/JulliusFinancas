import { ChangeDetectionStrategy, Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDatepickerModule } from '@angular/material/datepicker';
// O app não provê DateAdapter na raiz: cada feature importa o MatNativeDateModule.
// Sem ele, o datepicker quebra com "Cannot read properties of null (reading 'localeChanges')".
import { MatNativeDateModule } from '@angular/material/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { finalize } from 'rxjs/operators';
import { ConfirmDeleteDialogComponent } from '../../../../shared/components/confirm-delete-dialog/confirm-delete-dialog.component';
import { extractApiError } from '../../services/api-error';
import {
  BankAccount,
  DiscoveredAccount,
  ReconciliationService
} from '../../services/reconciliation.service';

@Component({
  selector: 'app-bank-account-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTooltipModule,
    CurrencyPipe,
    DatePipe
  ],
  templateUrl: './bank-account-list.component.html',
  styleUrls: ['./bank-account-list.component.scss']
})
export class BankAccountListComponent implements OnInit {
  private readonly service = inject(ReconciliationService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  readonly accounts = signal<BankAccount[]>([]);
  readonly discovered = signal<DiscoveredAccount[]>([]);
  readonly loading = signal(false);
  readonly discovering = signal(false);
  readonly syncing = signal(false);
  readonly busyAccountId = signal<string | null>(null);

  /** Data usada como marco zero. O primeiro sync do projeto parte de 01/08/2026. */
  openingBalanceDate = new Date(2026, 6, 31);
  itemIdInput = '';

  readonly hasAccounts = computed(() => this.accounts().length > 0);
  readonly pendingOpeningBalance = computed(() =>
    this.accounts().filter(account => !account.hasOpeningBalance).length
  );
  readonly consolidated = computed(() =>
    this.accounts().reduce((total, account) => total + account.lastKnownBalance, 0)
  );

  ngOnInit(): void {
    this.loadAccounts();
  }

  loadAccounts(): void {
    this.loading.set(true);
    this.service
      .getAccounts()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: accounts => this.accounts.set(accounts),
        error: error => this.showError('Erro ao carregar contas', error)
      });
  }

  checkConnections(): void {
    this.loading.set(true);
    this.service
      .checkConnections()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: accounts => {
          this.accounts.set(accounts);
          const broken = accounts.filter(account => account.isConnectionAlive === false).length;
          if (broken > 0) {
            this.snackBar.open(
              `${broken} conexão(ões) perdida(s). Reconecte no Meu Pluggy e atualize o itemId.`,
              'Fechar',
              { duration: 8000, panelClass: 'error-snackbar' }
            );
          } else {
            this.snackBar.open('Todas as conexões estão ativas.', 'Fechar', {
              duration: 4000,
              panelClass: 'success-snackbar'
            });
          }
        },
        error: error => this.showError('Erro ao verificar conexões', error)
      });
  }

  discoverAccounts(): void {
    const itemId = this.itemIdInput.trim();
    if (!itemId) {
      this.snackBar.open('Informe o itemId copiado do dashboard da Pluggy.', 'Fechar', { duration: 4000 });
      return;
    }

    this.discovering.set(true);
    this.service
      .discoverAccounts(itemId)
      .pipe(finalize(() => this.discovering.set(false)))
      .subscribe({
        next: accounts => {
          this.discovered.set(accounts);
          if (accounts.length === 0) {
            this.snackBar.open('Nenhuma conta encontrada para esse item.', 'Fechar', { duration: 4000 });
          }
        },
        error: error => this.showError('Erro ao consultar a Pluggy', error)
      });
  }

  registerDiscovered(account: DiscoveredAccount): void {
    this.service
      .createAccount({
        name: account.name,
        institution: account.name,
        holderName: account.owner ?? '',
        pluggyItemId: this.itemIdInput.trim(),
        pluggyAccountId: account.pluggyAccountId
      })
      .subscribe({
        next: () => {
          this.snackBar.open('Conta cadastrada com sucesso!', 'Fechar', {
            duration: 3000,
            panelClass: 'success-snackbar'
          });
          this.discovered.update(list =>
            list.map(item =>
              item.pluggyAccountId === account.pluggyAccountId
                ? { ...item, alreadyRegistered: true }
                : item
            )
          );
          this.loadAccounts();
        },
        error: error => this.showError('Erro ao cadastrar conta', error)
      });
  }

  setOpeningBalance(account: BankAccount): void {
    this.busyAccountId.set(account.id);
    this.service
      .setOpeningBalance(account.id, this.openingBalanceDate)
      .pipe(finalize(() => this.busyAccountId.set(null)))
      .subscribe({
        next: updated => {
          this.snackBar.open(
            `Marco zero definido: saldo anterior de ${updated.openingBalance.toFixed(2)}.`,
            'Fechar',
            { duration: 5000, panelClass: 'success-snackbar' }
          );
          this.loadAccounts();
        },
        error: error => this.showError('Erro ao definir marco zero', error)
      });
  }

  clearOpeningBalance(account: BankAccount): void {
    const dialogRef = this.dialog.open(ConfirmDeleteDialogComponent, {
      width: '450px',
      data: {
        entityName: 'marco zero',
        itemDescription: account.name,
        warningMessage: 'O lançamento "Saldo anterior" gerado para esta conta será removido.'
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) {
        return;
      }

      this.busyAccountId.set(account.id);
      this.service
        .clearOpeningBalance(account.id)
        .pipe(finalize(() => this.busyAccountId.set(null)))
        .subscribe({
          next: () => {
            this.snackBar.open('Marco zero removido.', 'Fechar', { duration: 3000 });
            this.loadAccounts();
          },
          error: error => this.showError('Erro ao remover marco zero', error)
        });
    });
  }

  deleteAccount(account: BankAccount): void {
    const dialogRef = this.dialog.open(ConfirmDeleteDialogComponent, {
      width: '450px',
      data: {
        entityName: 'conta bancária',
        itemDescription: account.name,
        details: [
          { label: 'Instituição', value: account.institution },
          { label: 'Saldo', value: account.lastKnownBalance.toFixed(2) }
        ]
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) {
        return;
      }

      this.service.deleteAccount(account.id).subscribe({
        next: () => {
          this.snackBar.open('Conta removida.', 'Fechar', { duration: 3000 });
          this.loadAccounts();
        },
        error: error => this.showError('Erro ao remover conta', error)
      });
    });
  }

  sync(): void {
    this.syncing.set(true);
    this.service
      .sync(this.openingBalanceDate)
      .pipe(finalize(() => this.syncing.set(false)))
      .subscribe({
        next: result => {
          result.warnings.forEach(warning =>
            this.snackBar.open(warning, 'Fechar', { duration: 8000 })
          );

          if (result.sessionId) {
            this.router.navigate(['/reconciliation/review', result.sessionId]);
            return;
          }

          this.snackBar.open(result.message, 'Fechar', { duration: 5000 });
          this.loadAccounts();
        },
        error: error => this.showError('Erro ao sincronizar', error)
      });
  }

  private showError(prefix: string, error: unknown): void {
    this.snackBar.open(`${prefix}: ${extractApiError(error)}`, 'Fechar', {
      duration: 8000,
      panelClass: 'error-snackbar'
    });
  }
}
