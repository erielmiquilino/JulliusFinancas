import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subscription } from 'rxjs';
import { FinancialTransactionService, FinancialTransaction, TransactionType } from '../../services/financial-transaction.service';
import { CreateTransactionDialogComponent } from '../create-transaction-dialog/create-transaction-dialog.component';
import { DeleteTransactionDialogComponent } from '../delete-transaction-dialog/delete-transaction-dialog.component';
import { EditTransactionDialogComponent } from '../edit-transaction-dialog/edit-transaction-dialog.component';

@Component({
  selector: 'app-transaction-list',
  templateUrl: './transaction-list.component.html',
  styleUrls: ['./transaction-list.component.scss']
})
export class TransactionListComponent implements OnInit, OnDestroy {
  displayedColumns: string[] = ['description', 'amount', 'dueDate', 'type', 'actions'];
  dataSource: MatTableDataSource<FinancialTransaction>;
  TransactionType = TransactionType;
  private refreshSubscription: Subscription;
  totalAmount: number = 0;

  // Filtros
  selectedMonth: number;
  selectedYear: number;
  selectedType: TransactionType = TransactionType.PayableBill;
  months = [
    { value: 1, label: 'Janeiro' },
    { value: 2, label: 'Fevereiro' },
    { value: 3, label: 'Março' },
    { value: 4, label: 'Abril' },
    { value: 5, label: 'Maio' },
    { value: 6, label: 'Junho' },
    { value: 7, label: 'Julho' },
    { value: 8, label: 'Agosto' },
    { value: 9, label: 'Setembro' },
    { value: 10, label: 'Outubro' },
    { value: 11, label: 'Novembro' },
    { value: 12, label: 'Dezembro' }
  ];

  @ViewChild(MatSort) sort!: MatSort;

  constructor(
    private transactionService: FinancialTransactionService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {
    this.dataSource = new MatTableDataSource<FinancialTransaction>();
    this.refreshSubscription = this.transactionService.refresh$.subscribe(() => {
      this.loadTransactions();
    });

    // Inicializa com o mês e ano atual
    const currentDate = new Date();
    this.selectedMonth = currentDate.getMonth() + 1;
    this.selectedYear = currentDate.getFullYear();
  }

  ngOnInit(): void {
    this.loadTransactions();
  }

  ngOnDestroy(): void {
    if (this.refreshSubscription) {
      this.refreshSubscription.unsubscribe();
    }
  }

  ngAfterViewInit() {
    this.dataSource.sort = this.sort;
  }

  calculateTotal(transactions: FinancialTransaction[]): void {
    this.totalAmount = transactions.reduce((total, transaction) => {
      // Soma positiva para Contas a Receber e negativa para Contas a Pagar
      const value = transaction.type === TransactionType.ReceivableBill ?
        transaction.amount : -transaction.amount;
      return total + value;
    }, 0);
  }

  openCreateDialog(): void {
    const dialogRef = this.dialog.open(CreateTransactionDialogComponent, {
      width: '500px',
      disableClose: true
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.snackBar.open('Transação criada com sucesso!', 'Fechar', {
          duration: 3000
        });
      }
    });
  }

  openDeleteDialog(transaction: FinancialTransaction): void {
    const dialogRef = this.dialog.open(DeleteTransactionDialogComponent, {
      width: '400px',
      data: transaction
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.deleteTransaction(transaction.id);
      }
    });
  }

  private deleteTransaction(id: string): void {
    this.transactionService.deleteTransaction(id).subscribe({
      next: () => {
        this.snackBar.open('Transação excluída com sucesso!', 'Fechar', {
          duration: 3000
        });
      },
      error: (error) => {
        this.snackBar.open('Erro ao excluir transação: ' + error.message, 'Fechar', {
          duration: 5000
        });
      }
    });
  }

  loadTransactions(): void {
    const filters = {
      month: this.selectedMonth,
      year: this.selectedYear,
      type: this.selectedType
    };

    this.transactionService.getAllTransactions(filters).subscribe({
      next: (transactions) => {
        this.dataSource.data = transactions;
        this.dataSource.sort = this.sort;
        this.calculateTotal(transactions);
      },
      error: (error) => {
        console.error('Erro ao carregar transações:', error);
        this.snackBar.open('Erro ao carregar transações: ' + error.message, 'Fechar', {
          duration: 5000
        });
      }
    });
  }

  onMonthChange(month: number): void {
    this.selectedMonth = month;
    this.loadTransactions();
  }

  onTypeChange(type: TransactionType): void {
    this.selectedType = type;
    this.loadTransactions();
  }

  getTransactionTypeLabel(type: TransactionType): string {
    return type === TransactionType.PayableBill ? 'Conta a Pagar' : 'Conta a Receber';
  }

  applyFilter(event: Event): void {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

  onEdit(transaction: FinancialTransaction): void {
    const dialogRef = this.dialog.open(EditTransactionDialogComponent, {
      width: '500px',
      data: transaction
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.transactionService.updateTransaction(transaction.id, result)
          .subscribe({
            next: () => {
              this.loadTransactions();
            },
            error: (error) => {
              console.error('Erro ao atualizar transação:', error);
            }
          });
      }
    });
  }
}
