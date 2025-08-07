import { Component, OnInit, OnDestroy, ViewChild, AfterViewInit, ChangeDetectorRef } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subscription } from 'rxjs';
import { FinancialTransactionService, FinancialTransaction, TransactionType, TransactionFilters } from '../../services/financial-transaction.service';
import { CreateTransactionDialogComponent } from '../create-transaction-dialog/create-transaction-dialog.component';
import { DeleteTransactionDialogComponent } from '../delete-transaction-dialog/delete-transaction-dialog.component';
import { EditTransactionDialogComponent } from '../edit-transaction-dialog/edit-transaction-dialog.component';
import { MatPaginator } from '@angular/material/paginator';

@Component({
  selector: 'app-transaction-list',
  templateUrl: './transaction-list.component.html',
  styleUrls: ['./transaction-list.component.scss']
})
export class TransactionListComponent implements OnInit, OnDestroy, AfterViewInit {
  displayedColumns: string[] = ['description', 'amount', 'dueDate', 'type', 'isPaid', 'actions'];
  dataSource: MatTableDataSource<FinancialTransaction>;
  TransactionType = TransactionType;
  private refreshSubscription: Subscription;
  totalAmount: number = 0;

  // Filtros
  selectedMonth: number;
  selectedYear: number;
  selectedType: TransactionType = TransactionType.PayableBill;
  selectedDateRangeType: 'Today' | 'ThisWeek' | 'Month' | 'Custom' = 'Month';
  startDate?: Date;
  endDate?: Date;
  selectedPaymentStatus: 'Paid' | 'Pending' | 'All' = 'All';
  currentYear: number;

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
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  constructor(
    private transactionService: FinancialTransactionService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {
    this.dataSource = new MatTableDataSource<FinancialTransaction>();
    this.dataSource.filterPredicate = (data: FinancialTransaction, filter: string) => {
      const normalized = filter.trim().toLowerCase();
      return data.description?.toLowerCase().includes(normalized);
    };
    this.refreshSubscription = this.transactionService.refresh$.subscribe(() => {
      this.loadTransactions();
    });

    const currentDate = new Date();
    this.selectedMonth = currentDate.getMonth() + 1;
    this.selectedYear = currentDate.getFullYear();
    this.currentYear = currentDate.getFullYear();
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
    setTimeout(() => {
      this.dataSource.sort = this.sort;
      this.dataSource.paginator = this.paginator;
      if (this.sort) {
        this.sort.active = 'dueDate';
        this.sort.direction = 'desc';
      }
      this.cdr.detectChanges();
    });
  }

  calculateTotal(transactions: FinancialTransaction[]): void {
    this.totalAmount = transactions.reduce((total, transaction) => {
      const value = transaction.type === TransactionType.ReceivableBill ?
        transaction.amount : -transaction.amount;
      return total + value;
    }, 0);
  }

  loadTransactions(): void {
    const filters: TransactionFilters = {
      type: this.selectedType,
      dateRangeType: this.selectedDateRangeType,
      paymentStatus: this.selectedPaymentStatus
    };

    if (this.selectedDateRangeType === 'Month') {
      filters.month = this.selectedMonth;
      filters.year = this.selectedYear;
    } else if (this.selectedDateRangeType === 'Custom' && this.startDate && this.endDate) {
      filters.startDate = this.startDate;
      filters.endDate = this.endDate;
    }

    this.transactionService.getAllTransactions(filters).subscribe({
      next: (transactions) => {
        this.dataSource.data = transactions;
        if (this.paginator) {
          this.dataSource.paginator = this.paginator;
        }
        if (this.sort) {
          this.dataSource.sort = this.sort;
        }
        this.calculateTotal(this.dataSource.filteredData);
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
    this.selectedDateRangeType = 'Month';
    this.loadTransactions();
  }

  onYearChange(year: number): void {
    this.selectedYear = year;
    this.selectedDateRangeType = 'Month';
    this.loadTransactions();
  }

  onDateRangeTypeChange(type: 'Today' | 'ThisWeek' | 'Month' | 'Custom'): void {
    this.selectedDateRangeType = type;
    if (type !== 'Month') {
    } else {
      if (!this.selectedMonth || !this.selectedYear) {
        const currentDate = new Date();
        this.selectedMonth = currentDate.getMonth() + 1;
        this.selectedYear = currentDate.getFullYear();
      }
    }
    if (type !== 'Custom') {
      this.startDate = undefined;
      this.endDate = undefined;
    }
    this.loadTransactions();
  }

  onStartDateChange(event: any): void {
    this.startDate = event.value;
    if (this.startDate && this.endDate && this.selectedDateRangeType === 'Custom') {
      this.loadTransactions();
    }
  }

  onEndDateChange(event: any): void {
    this.endDate = event.value;
    if (this.startDate && this.endDate && this.selectedDateRangeType === 'Custom') {
      this.loadTransactions();
    }
  }

  onPaymentStatusChange(status: 'Paid' | 'Pending' | 'All'): void {
    this.selectedPaymentStatus = status;
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
    this.calculateTotal(this.dataSource.filteredData);
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
              this.snackBar.open('Transação atualizada com sucesso!', 'Fechar', {
                duration: 3000
              });
            },
            error: (error) => {
              console.error('Erro ao atualizar transação:', error);
              this.snackBar.open('Erro ao atualizar transação.', 'Fechar', {
                duration: 3000
              });
            }
          });
      }
    });
  }

  async onPaymentStatusChangeToggle(id: string, isPaid: boolean): Promise<void> {
    try {
      await this.transactionService.updatePaymentStatus(id, isPaid).toPromise();
      this.snackBar.open(`Status de pagamento atualizado para ${isPaid ? 'Pago' : 'Pendente'}!`, 'Fechar', {
        duration: 2000
      });
    } catch (error) {
      console.error('Erro ao atualizar status de pagamento:', error);
      this.snackBar.open('Erro ao atualizar status de pagamento.', 'Fechar', {
        duration: 3000
      });
      this.loadTransactions();
    }
  }
}
