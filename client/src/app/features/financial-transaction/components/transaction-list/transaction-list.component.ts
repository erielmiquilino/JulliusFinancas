import { Component, OnInit, OnDestroy, ViewChild, AfterViewInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatExpansionModule } from '@angular/material/expansion';
import { BreakpointObserver } from '@angular/cdk/layout';
import { Subscription, forkJoin, of, Subject } from 'rxjs';
import { catchError, takeUntil } from 'rxjs/operators';
import { FinancialTransactionService, FinancialTransaction, TransactionType, TransactionFilters } from '../../services/financial-transaction.service';
import { CreateTransactionDialogComponent } from '../create-transaction-dialog/create-transaction-dialog.component';
import { ConfirmDeleteDialogComponent } from '../../../../shared/components/confirm-delete-dialog/confirm-delete-dialog.component';
import { EditTransactionDialogComponent } from '../edit-transaction-dialog/edit-transaction-dialog.component';
import { PayWithCardDialogComponent } from '../pay-with-card-dialog/pay-with-card-dialog.component';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { FilterStorageService } from '../../../../shared/services/filter-storage.service';

interface TransactionFilterState {
  selectedMonth: number;
  selectedYear: number;
  selectedType: TransactionType;
  selectedDateRangeType: 'Today' | 'ThisWeek' | 'Month' | 'Custom';
  startDate?: string;
  endDate?: string;
  selectedPaymentStatus: 'Paid' | 'Pending' | 'All';
  textFilter?: string;
}

@Component({
  selector: 'app-transaction-list',
  templateUrl: './transaction-list.component.html',
  styleUrls: ['./transaction-list.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatDialogModule,
    MatSnackBarModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatDatepickerModule,
    MatCheckboxModule,
    MatSlideToggleModule,
    MatTooltipModule,
    MatExpansionModule,
    CurrencyPipe,
    DatePipe
  ]
})
export class TransactionListComponent implements OnInit, OnDestroy, AfterViewInit {
  private readonly FILTER_STORAGE_KEY = 'transaction-list-filters';
  private destroy$ = new Subject<void>();

  // Colunas para desktop e mobile
  private readonly desktopColumns: string[] = ['select', 'description', 'category', 'amount', 'dueDate', 'isPaid'];
  private readonly mobileColumns: string[] = ['select', 'description', 'amount'];

  displayedColumns: string[] = this.desktopColumns;
  dataSource: MatTableDataSource<FinancialTransaction>;
  TransactionType = TransactionType;
  private refreshSubscription: Subscription;
  totalAmount: number = 0;
  selectedAmount: number = 0;
  selectedTransactions: Set<string> = new Set<string>();
  isMobile = false;

  // Filtros
  selectedMonth: number;
  selectedYear: number;
  selectedType: TransactionType = TransactionType.PayableBill;
  selectedDateRangeType: 'Today' | 'ThisWeek' | 'Month' | 'Custom' = 'Month';
  startDate?: Date;
  endDate?: Date;
  selectedPaymentStatus: 'Paid' | 'Pending' | 'All' = 'All';
  currentYear: number;
  textFilter: string = '';

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
    private cdr: ChangeDetectorRef,
    private filterStorage: FilterStorageService,
    private breakpointObserver: BreakpointObserver
  ) {
    this.dataSource = new MatTableDataSource<FinancialTransaction>();
    this.dataSource.filterPredicate = (data: FinancialTransaction, filter: string) => {
      const normalized = filter.trim().toLowerCase();
      const matchesDescription = data.description?.toLowerCase().includes(normalized) ?? false;
      const matchesCategory = data.category?.name?.toLowerCase().includes(normalized) ?? false;
      return matchesDescription || matchesCategory;
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
    this.loadFiltersFromStorage();
    this.loadTransactions();

    // Observa mudanças de breakpoint para ajustar colunas
    this.breakpointObserver
      .observe(['(max-width: 768px)'])
      .pipe(takeUntil(this.destroy$))
      .subscribe(result => {
        this.isMobile = result.matches;
        this.displayedColumns = this.isMobile ? this.mobileColumns : this.desktopColumns;
        this.cdr.detectChanges();
      });
  }

  ngOnDestroy(): void {
    if (this.refreshSubscription) {
      this.refreshSubscription.unsubscribe();
    }
    this.destroy$.next();
    this.destroy$.complete();
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

  calculateSelectedTotal(): void {
    const selectedIds = Array.from(this.selectedTransactions);
    const selectedTransactionsData = this.dataSource.data.filter(t => selectedIds.includes(t.id));

    this.selectedAmount = selectedTransactionsData.reduce((total, transaction) => {
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
    this.saveFiltersToStorage();
    this.loadTransactions();
  }

  onYearChange(year: number): void {
    this.selectedYear = year;
    this.selectedDateRangeType = 'Month';
    this.saveFiltersToStorage();
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
    this.saveFiltersToStorage();
    this.loadTransactions();
  }

  onStartDateChange(event: any): void {
    this.startDate = event.value;
    if (this.startDate && this.endDate && this.selectedDateRangeType === 'Custom') {
      this.saveFiltersToStorage();
      this.loadTransactions();
    }
  }

  onEndDateChange(event: any): void {
    this.endDate = event.value;
    if (this.startDate && this.endDate && this.selectedDateRangeType === 'Custom') {
      this.saveFiltersToStorage();
      this.loadTransactions();
    }
  }

  onPaymentStatusChange(status: 'Paid' | 'Pending' | 'All'): void {
    this.selectedPaymentStatus = status;
    this.saveFiltersToStorage();
    this.loadTransactions();
  }

  onTypeChange(type: TransactionType): void {
    this.selectedType = type;
    this.saveFiltersToStorage();
    this.loadTransactions();
  }

  getTransactionTypeLabel(type: TransactionType): string {
    return type === TransactionType.PayableBill ? 'Conta a Pagar' : 'Conta a Receber';
  }

  applyFilter(event: Event): void {
    const filterValue = (event.target as HTMLInputElement).value;
    this.textFilter = filterValue;
    this.dataSource.filter = filterValue.trim().toLowerCase();
    this.calculateTotal(this.dataSource.filteredData);
    this.saveFiltersToStorage();
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
    const dialogRef = this.dialog.open(ConfirmDeleteDialogComponent, {
      width: '400px',
      data: {
        entityName: 'a transação',
        itemDescription: transaction.description
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.deleteTransaction(transaction.id);
      }
    });
  }

  deleteSelected(): void {
    const selectedIds = Array.from(this.selectedTransactions);
    if (selectedIds.length === 0) {
      this.snackBar.open('Nenhuma transação selecionada.', 'Fechar', { duration: 3000 });
      return;
    }

    const selectedTransactionsData = this.dataSource.data.filter(t => selectedIds.includes(t.id));

    const dialogRef = this.dialog.open(ConfirmDeleteDialogComponent, {
      width: '450px',
      data: {
        entityName: 'transações',
        itemDescription: '',
        items: selectedTransactionsData.map(t => t.description)
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.transactionService.deleteTransactions(selectedIds).subscribe({
          next: (response) => {
            this.snackBar.open(
              `${response.deletedCount} transação(ões) excluída(s) com sucesso!`,
              'Fechar',
              { duration: 3000 }
            );
            this.selectedTransactions.clear();
            this.calculateSelectedTotal();
          },
          error: (error) => {
            this.snackBar.open('Erro ao excluir transações: ' + error.message, 'Fechar', {
              duration: 5000
            });
          }
        });
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

  // Métodos de seleção
  isAllSelected(): boolean {
    const numSelected = this.selectedTransactions.size;
    const numRows = this.dataSource.data.length;
    return numSelected === numRows && numRows > 0;
  }

  isSomeSelected(): boolean {
    return this.selectedTransactions.size > 0 && !this.isAllSelected();
  }

  toggleAllRows(): void {
    if (this.isAllSelected()) {
      this.selectedTransactions.clear();
    } else {
      this.dataSource.data.forEach(row => this.selectedTransactions.add(row.id));
    }
    this.calculateSelectedTotal();
  }

  toggleRow(id: string): void {
    if (this.selectedTransactions.has(id)) {
      this.selectedTransactions.delete(id);
    } else {
      this.selectedTransactions.add(id);
    }
    this.calculateSelectedTotal();
  }

  isSelected(id: string): boolean {
    return this.selectedTransactions.has(id);
  }

  hasSelectedTransactions(): boolean {
    return this.selectedTransactions.size > 0;
  }

  // Métodos de verificação de status de vencimento
  isOverdue(transaction: FinancialTransaction): boolean {
    if (transaction.isPaid) {
      return false;
    }
    const dueDateKey = this.getTransactionDueDateKey(transaction);
    const todayKey = this.getTodayLocalDateKey();
    return dueDateKey < todayKey;
  }

  isDueToday(transaction: FinancialTransaction): boolean {
    if (transaction.isPaid) {
      return false;
    }
    const dueDateKey = this.getTransactionDueDateKey(transaction);
    const todayKey = this.getTodayLocalDateKey();
    return dueDateKey === todayKey;
  }

  getDueDateStatus(transaction: FinancialTransaction): 'overdue' | 'due-today' | 'normal' {
    if (this.isOverdue(transaction)) {
      return 'overdue';
    }
    if (this.isDueToday(transaction)) {
      return 'due-today';
    }
    return 'normal';
  }

  private getTransactionDueDateKey(transaction: FinancialTransaction): string {
    const rawDueDate = transaction.dueDate as unknown;

    if (typeof rawDueDate === 'string') {
      const isoMatch = rawDueDate.match(/^(\d{4}-\d{2}-\d{2})/);
      if (isoMatch) {
        return isoMatch[1];
      }
    }

    const parsedDate = new Date(transaction.dueDate);
    return this.toLocalDateKey(parsedDate);
  }

  private getTodayLocalDateKey(): string {
    return this.toLocalDateKey(new Date());
  }

  private toLocalDateKey(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private saveFiltersToStorage(): void {
    this.filterStorage.save<TransactionFilterState>(this.FILTER_STORAGE_KEY, {
      selectedMonth: this.selectedMonth,
      selectedYear: this.selectedYear,
      selectedType: this.selectedType,
      selectedDateRangeType: this.selectedDateRangeType,
      startDate: this.startDate ? this.startDate.toISOString() : undefined,
      endDate: this.endDate ? this.endDate.toISOString() : undefined,
      selectedPaymentStatus: this.selectedPaymentStatus,
      textFilter: this.textFilter
    });
  }

  private loadFiltersFromStorage(): void {
    const filterState = this.filterStorage.load<TransactionFilterState>(this.FILTER_STORAGE_KEY);
    if (filterState) {
      this.selectedMonth = filterState.selectedMonth;
      this.selectedYear = filterState.selectedYear;
      this.selectedType = filterState.selectedType;
      this.selectedDateRangeType = filterState.selectedDateRangeType;
      this.selectedPaymentStatus = filterState.selectedPaymentStatus;
      this.textFilter = filterState.textFilter || '';

      if (filterState.startDate) {
        this.startDate = new Date(filterState.startDate);
      }
      if (filterState.endDate) {
        this.endDate = new Date(filterState.endDate);
      }

      // Aplica o filtro de texto se houver
      if (this.textFilter) {
        this.dataSource.filter = this.textFilter.trim().toLowerCase();
      }
    }
  }

  onPayWithCard(): void {
    const selectedIds = Array.from(this.selectedTransactions);
    const selectedTransactionsData = this.dataSource.data.filter(t => selectedIds.includes(t.id));

    // Calcula o total das transações selecionadas
    const totalAmount = selectedTransactionsData.reduce((total, transaction) => {
      return total + transaction.amount;
    }, 0);

    const dialogRef = this.dialog.open(PayWithCardDialogComponent, {
      width: '500px',
      disableClose: true,
      data: {
        selectedTransactionIds: selectedIds,
        totalAmount: totalAmount
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        // Por enquanto apenas exibe mensagem de sucesso
        // A lógica de persistência será implementada posteriormente
        console.log('Pagamento com cartão confirmado:', result);
        this.snackBar.open('Pagamento registrado com sucesso!', 'Fechar', {
          duration: 3000
        });
        // Limpa as seleções
        this.selectedTransactions.clear();
      }
    });
  }

  markSelectedAsPaid(): void {
    const selectedIds = Array.from(this.selectedTransactions);

    if (selectedIds.length === 0) {
      this.snackBar.open('Nenhuma transação selecionada.', 'Fechar', {
        duration: 3000
      });
      return;
    }

    // Filtra apenas transações que ainda não estão pagas
    const selectedTransactionsData = this.dataSource.data.filter(t =>
      selectedIds.includes(t.id) && !t.isPaid
    );

    if (selectedTransactionsData.length === 0) {
      this.snackBar.open('Todas as transações selecionadas já estão pagas.', 'Fechar', {
        duration: 3000
      });
      return;
    }

    // Mostra feedback de processamento
    const processingMessage = this.snackBar.open(
      `Processando ${selectedTransactionsData.length} transação(ões)...`,
      'Fechar',
      { duration: 2000 }
    );

    // Cria array de observables para processar em paralelo
    const updateObservables = selectedTransactionsData.map(transaction =>
      this.transactionService.updatePaymentStatus(transaction.id, true).pipe(
        catchError(error => {
          console.error(`Erro ao atualizar transação ${transaction.id}:`, error);
          return of({ id: transaction.id, error: error.message || 'Erro desconhecido' });
        })
      )
    );

    // Processa todas as atualizações em paralelo
    forkJoin(updateObservables).subscribe({
      next: (results) => {
        processingMessage.dismiss();

        // Conta sucessos e falhas
        const successes = results.filter(r => !('error' in r));
        const failures = results.filter(r => 'error' in r);

        if (failures.length === 0) {
          // Todas as atualizações foram bem-sucedidas
          this.snackBar.open(
            `${successes.length} transação(ões) marcada(s) como paga(s) com sucesso!`,
            'Fechar',
            { duration: 3000 }
          );
        } else {
          // Algumas falharam
          this.snackBar.open(
            `${successes.length} transação(ões) atualizada(s) com sucesso. ${failures.length} falha(ram).`,
            'Fechar',
            { duration: 5000 }
          );
        }

        // Limpa as seleções
        this.selectedTransactions.clear();
        this.calculateSelectedTotal();

        // Recarrega a lista de transações
        this.loadTransactions();
      },
      error: (error) => {
        processingMessage.dismiss();
        console.error('Erro ao processar atualizações:', error);
        this.snackBar.open('Erro ao processar atualizações de status.', 'Fechar', {
          duration: 5000
        });
        // Recarrega a lista mesmo em caso de erro
        this.loadTransactions();
      }
    });
  }
}
