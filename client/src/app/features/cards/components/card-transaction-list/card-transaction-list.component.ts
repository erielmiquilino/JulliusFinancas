import { Component, OnInit, OnDestroy, ViewChild, AfterViewInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subscription } from 'rxjs';
import { CardService, Card, CardTransaction } from '../../services/card.service';
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
  isLoading = false;

  // Propriedades para o sistema de faturas
  invoiceOptions: { value: string, label: string }[] = [];
  selectedInvoice: string = '';

  @ViewChild(MatSort) sort!: MatSort;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private cardService: CardService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
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
        this.fetchTransactions();
      } else {
        this.snackBar.open('ID do cartão não encontrado', 'Fechar', { duration: 3000 });
        this.router.navigate(['/cards']);
      }
    });
  }

  ngAfterViewInit(): void {
    this.dataSource.sort = this.sort;
    if (this.sort) {
      this.sort.active = 'date';
      this.sort.direction = 'desc';
    }
  }

  ngOnDestroy(): void {
    if (this.refreshSubscription) {
      this.refreshSubscription.unsubscribe();
    }
  }

  loadCard(): void {
    this.cardService.getCardById(this.cardId).subscribe({
      next: (card) => {
        this.card = card;
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

    // Define o mês/ano atual como selecionado por padrão
    this.selectedInvoice = `${currentMonth}-${currentYear}`;
  }

  fetchTransactions(): void {
    if (!this.cardId || !this.selectedInvoice) return;

    this.isLoading = true;
    const [month, year] = this.selectedInvoice.split('-').map(Number);

    this.cardService.getTransactionsForInvoice(this.cardId, month - 1, year).subscribe({
      next: (transactions) => {
        this.dataSource.data = transactions;
        this.dataSource.sort = this.sort;
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
        this.dataSource.sort = this.sort;
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
    const dialogRef = this.dialog.open(CreateCardTransactionDialogComponent, {
      width: '500px',
      disableClose: true,
      data: { cardId: this.cardId }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.snackBar.open('Lançamento criado com sucesso!', 'Fechar', {
          duration: 3000
        });
      }
    });
  }

  openEditDialog(transaction: CardTransaction): void {
    const dialogRef = this.dialog.open(EditCardTransactionDialogComponent, {
      width: '500px',
      disableClose: true,
      data: transaction
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.snackBar.open('Lançamento atualizado com sucesso!', 'Fechar', {
          duration: 3000
        });
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

  private deleteTransaction(id: string): void {
    this.cardService.deleteCardTransaction(id).subscribe({
      next: () => {
        this.snackBar.open('Lançamento excluído com sucesso!', 'Fechar', {
          duration: 3000
        });
      },
      error: (error) => {
        console.error('Erro ao excluir lançamento:', error);
        this.snackBar.open('Erro ao excluir lançamento: ' + error.message, 'Fechar', {
          duration: 5000
        });
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/cards']);
  }
}
