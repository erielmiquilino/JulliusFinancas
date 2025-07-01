import { Component, OnInit, OnDestroy, ViewChild, AfterViewInit, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subscription } from 'rxjs';
import { CardService, Card, CardTransaction, CardTransactionType, UpdateCardTransactionRequest } from '../../services/card.service';
import { CreateCardTransactionDialogComponent } from '../create-card-transaction-dialog/create-card-transaction-dialog.component';
import { EditCardTransactionDialogComponent } from '../edit-card-transaction-dialog/edit-card-transaction-dialog.component';
import { DeleteCardTransactionDialogComponent } from '../delete-card-transaction-dialog/delete-card-transaction-dialog.component';

@Component({
  selector: 'app-card-transaction-list',
  templateUrl: './card-transaction-list.component.html',
  styleUrls: ['./card-transaction-list.component.scss']
})
export class CardTransactionListComponent implements OnInit, OnDestroy, AfterViewInit {
  displayedColumns: string[] = ['description', 'amount', 'date', 'installment', 'actions'];
  dataSource: MatTableDataSource<CardTransaction>;
  private refreshSubscription: Subscription;

  cardId: string = '';
  card: Card | null = null;
  isLoading = true;

  // Propriedades para o sistema de faturas
  invoiceOptions: { value: string, label: string }[] = [];
  selectedInvoice: string = '';

  // Propriedades para informações da fatura
  currentLimit: number = 0;
  invoiceTotal: number = 0;

  @ViewChild(MatSort) sort!: MatSort;

  CardTransactionType = CardTransactionType;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private cardService: CardService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {
    this.dataSource = new MatTableDataSource<CardTransaction>();
    this.refreshSubscription = this.cardService.refresh$.subscribe(() => {
      this.fetchTransactions();
    });
  }

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.cardId = params['id'];
      if (this.cardId) {
        this.generateInvoiceOptions();
        this.loadCard();
      } else {
        this.snackBar.open('ID do cartão não encontrado', 'Fechar', { duration: 3000 });
        this.router.navigate(['/cards']);
      }
    });
  }

  ngAfterViewInit(): void {
    setTimeout(() => {
      this.dataSource.sort = this.sort;
      if (this.sort) {
        this.sort.active = 'date';
        this.sort.direction = 'desc';
      }
      this.cdr.detectChanges();
    });
  }

  ngOnDestroy(): void {
    if (this.refreshSubscription) {
      this.refreshSubscription.unsubscribe();
    }
  }

  generateInvoiceOptions(): void {
    const currentDate = new Date();
    const currentMonth = currentDate.getMonth() + 1;
    const currentYear = currentDate.getFullYear();

    this.invoiceOptions = [];

    // Gera 12 meses para o passado + atual + 12 meses para o futuro (25 opções)
    for (let i = -12; i <= 12; i++) {
      const date = new Date(currentYear, currentMonth - 1 + i, 1);
      const month = date.getMonth() + 1;
      const year = date.getFullYear();

      const monthNames = [
        'janeiro', 'fevereiro', 'março', 'abril', 'maio', 'junho',
        'julho', 'agosto', 'setembro', 'outubro', 'novembro', 'dezembro'
      ];

      const display = `${monthNames[date.getMonth()]}/${year}`;

      this.invoiceOptions.push({
        value: `${month}-${year}`,
        label: display
      });
    }

    // Define a fatura atual como selecionada por padrão (será calculada quando o cartão for carregado)
    this.selectedInvoice = `${currentMonth}-${currentYear}`;
  }

  /**
   * Calcula o período da fatura atual baseado na data de hoje e nos dias de fechamento/vencimento do cartão
   * Replica a lógica do método CalculateInvoicePeriod do backend
   *
   * Exemplo: Cartão fecha dia 30 e vence dia 7
   * - Se hoje for 7 de junho (depois do fechamento de 30 de maio)
   * - A fatura que está aberta é de julho/2025 (que vence em 7 de julho)
   */
  private calculateCurrentInvoicePeriod(closingDay: number, dueDay: number): { year: number, month: number } {
    const today = new Date();

    let effectiveClosingDate: Date;

    if (today.getDate() > closingDay) {
      // Se hoje é depois do dia de fechamento, a data efetiva de fechamento é no próximo mês
      effectiveClosingDate = new Date(today.getFullYear(), today.getMonth() + 1, closingDay);
    } else {
      // Se hoje é antes ou no dia de fechamento, a data efetiva é neste mês
      effectiveClosingDate = new Date(today.getFullYear(), today.getMonth(), closingDay);
    }

    let invoiceDueDate: Date;

    if (dueDay <= closingDay) {
      // Se o vencimento é antes ou no dia de fechamento, vai para o próximo mês
      const monthOfDueDate = new Date(effectiveClosingDate.getFullYear(), effectiveClosingDate.getMonth() + 1, 1);
      invoiceDueDate = new Date(monthOfDueDate.getFullYear(), monthOfDueDate.getMonth(), dueDay);
    } else {
      // Se o vencimento é depois do fechamento, fica no mesmo mês do fechamento
      invoiceDueDate = new Date(effectiveClosingDate.getFullYear(), effectiveClosingDate.getMonth(), dueDay);
    }

    return {
      year: invoiceDueDate.getFullYear(),
      month: invoiceDueDate.getMonth() + 1 // +1 porque getMonth() retorna 0-11
    };
  }

  loadCard(): void {
    this.cardService.getCardById(this.cardId).subscribe({
      next: (card) => {
        this.card = card;

        // Calcula a fatura atual baseada nos dias de fechamento e vencimento do cartão
        const currentInvoice = this.calculateCurrentInvoicePeriod(card.closingDay, card.dueDay);
        this.selectedInvoice = `${currentInvoice.month}-${currentInvoice.year}`;

        // Carrega as transações da fatura atual
        this.fetchTransactions();
      },
      error: (error) => {
        console.error('Erro ao carregar cartão:', error);
        this.snackBar.open('Erro ao carregar informações do cartão: ' + error.message, 'Fechar', {
          duration: 5000
        });
        this.router.navigate(['/cards']);
      }
    });
  }

  fetchTransactions(): void {
    if (!this.cardId || !this.selectedInvoice) return;

    this.isLoading = true;
    const [month, year] = this.selectedInvoice.split('-').map(Number);

    this.cardService.getTransactionsForInvoice(this.cardId, month, year).subscribe({
      next: (response) => {
        this.dataSource.data = response.transactions;
        this.currentLimit = response.currentLimit;
        this.invoiceTotal = response.invoiceTotal;
        if (this.sort) {
          this.dataSource.sort = this.sort;
        }
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Erro ao carregar transações:', error);
        this.snackBar.open('Erro ao carregar transações: ' + error.message, 'Fechar', {
          duration: 5000
        });
        this.isLoading = false;
      }
    });
  }

  onInvoiceChange(): void {
    this.fetchTransactions();
  }

  loadTransactions(): void {
    this.isLoading = true;
    this.cardService.getTransactionsByCardId(this.cardId).subscribe({
      next: (transactions) => {
        this.dataSource.data = transactions;
        if (this.sort) {
          this.dataSource.sort = this.sort;
        }
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Erro ao carregar transações:', error);
        this.snackBar.open('Erro ao carregar transações: ' + error.message, 'Fechar', {
          duration: 5000
        });
        this.isLoading = false;
      }
    });
  }

  openCreateDialog(): void {
    const [month, year] = this.selectedInvoice.split('-').map(Number);
    const dialogRef = this.dialog.open(CreateCardTransactionDialogComponent, {
      width: '500px',
      disableClose: true,
      data: {
        cardId: this.cardId,
        invoiceYear: year,
        invoiceMonth: month
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.snackBar.open('Lançamento criado com sucesso!', 'Fechar', {
          duration: 3000
        });
        this.fetchTransactions(); // Recarrega a lista de transações
      }
    });
  }

  openEditDialog(transaction: CardTransaction): void {
    const [month, year] = this.selectedInvoice.split('-').map(Number);
    const dialogRef = this.dialog.open(EditCardTransactionDialogComponent, {
      width: '500px',
      disableClose: true,
      data: {
        ...transaction,
        invoiceYear: year,
        invoiceMonth: month
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.updateTransaction(transaction.id, result);
      }
    });
  }

  openDeleteDialog(transaction: CardTransaction): void {
    const dialogRef = this.dialog.open(DeleteCardTransactionDialogComponent, {
      width: '400px',
      data: transaction
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.deleteTransaction(transaction.id);
      }
    });
  }

  private updateTransaction(id: string, updateData: UpdateCardTransactionRequest): void {
    this.cardService.updateCardTransaction(id, updateData).subscribe({
      next: () => {
        this.snackBar.open('Lançamento atualizado com sucesso!', 'Fechar', {
          duration: 3000
        });
        this.fetchTransactions(); // Recarrega a lista de transações
      },
      error: (error) => {
        console.error('Erro ao atualizar lançamento:', error);
        this.snackBar.open('Erro ao atualizar lançamento: ' + error.message, 'Fechar', {
          duration: 5000
        });
      }
    });
  }

  private deleteTransaction(id: string): void {
    this.cardService.deleteCardTransaction(id).subscribe({
      next: () => {
        this.snackBar.open('Lançamento excluído com sucesso!', 'Fechar', {
          duration: 3000
        });
        this.fetchTransactions(); // Recarrega a lista de transações
      },
      error: (error) => {
        console.error('Erro ao excluir lançamento:', error);
        this.snackBar.open('Erro ao excluir lançamento: ' + error.message, 'Fechar', {
          duration: 5000
        });
      }
    });
  }

  moveTransactionToPreviousInvoice(transaction: CardTransaction): void {
    const [currentMonth, currentYear] = this.selectedInvoice.split('-').map(Number);
    const previousDate = new Date(currentYear, currentMonth - 2, 1); // -2 porque getMonth() é 0-based
    const previousMonth = previousDate.getMonth() + 1;
    const previousYear = previousDate.getFullYear();

    this.moveTransaction(transaction, previousYear, previousMonth, 'anterior');
  }

  moveTransactionToNextInvoice(transaction: CardTransaction): void {
    const [currentMonth, currentYear] = this.selectedInvoice.split('-').map(Number);
    const nextDate = new Date(currentYear, currentMonth, 1); // currentMonth porque queremos +1 mês
    const nextMonth = nextDate.getMonth() + 1;
    const nextYear = nextDate.getFullYear();

    this.moveTransaction(transaction, nextYear, nextMonth, 'próxima');
  }

  private moveTransaction(transaction: CardTransaction, invoiceYear: number, invoiceMonth: number, direction: string): void {
    this.cardService.moveTransactionToInvoice(transaction, invoiceYear, invoiceMonth).subscribe({
      next: () => {
        this.snackBar.open(`Transação movida para a fatura ${direction} com sucesso!`, 'Fechar', {
          duration: 3000
        });
        this.fetchTransactions(); // Recarrega a lista de transações
      },
      error: (error) => {
        console.error('Erro ao mover transação:', error);
        this.snackBar.open('Erro ao mover transação: ' + error.message, 'Fechar', {
          duration: 5000
        });
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/cards']);
  }
}
