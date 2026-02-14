import { Component, OnInit, OnDestroy, ViewChild, AfterViewInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatCardModule } from '@angular/material/card';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatExpansionModule } from '@angular/material/expansion';
import { Router } from '@angular/router';
import { BreakpointObserver } from '@angular/cdk/layout';
import { Subscription, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CardService, Card } from '../../services/card.service';
import { CreateCardDialogComponent } from '../create-card-dialog/create-card-dialog.component';
import { EditCardDialogComponent } from '../edit-card-dialog/edit-card-dialog.component';
import { ConfirmDeleteDialogComponent } from '../../../../shared/components/confirm-delete-dialog/confirm-delete-dialog.component';

@Component({
  selector: 'app-card-list',
  templateUrl: './card-list.component.html',
  styleUrls: ['./card-list.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatSortModule,
    MatDialogModule,
    MatSnackBarModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatCardModule,
    MatInputModule,
    MatFormFieldModule,
    MatExpansionModule,
    CurrencyPipe
  ]
})
export class CardListComponent implements OnInit, OnDestroy, AfterViewInit {
  private destroy$ = new Subject<void>();
  private readonly desktopColumns: string[] = ['name', 'issuingBank', 'closingDay', 'dueDay', 'limit', 'currentLimit', 'actions'];
  private readonly mobileColumns: string[] = ['name', 'currentLimit', 'actions'];

  displayedColumns: string[] = this.desktopColumns;
  dataSource: MatTableDataSource<Card>;
  private refreshSubscription: Subscription;
  isMobile = false;
  textFilter = '';

  @ViewChild(MatSort) sort!: MatSort;

  constructor(
    private cardService: CardService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private breakpointObserver: BreakpointObserver
  ) {
    this.dataSource = new MatTableDataSource<Card>();
    this.dataSource.filterPredicate = this.createFilter();
    this.refreshSubscription = this.cardService.refresh$.subscribe(() => {
      this.loadCards();
    });
  }

  ngOnInit(): void {
    this.loadCards();

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

  ngAfterViewInit(): void {
    setTimeout(() => {
      this.dataSource.sort = this.sort;
      if (this.sort) {
        this.sort.active = 'name';
        this.sort.direction = 'asc';
      }
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

  loadCards(): void {
    this.cardService.getCards().subscribe({
      next: (cards) => {
        this.dataSource.data = cards;
        if (this.sort) {
          this.dataSource.sort = this.sort;
        }
      },
      error: (error) => {
        console.error('Erro ao carregar cartões:', error);
        this.snackBar.open('Erro ao carregar cartões: ' + error.message, 'Fechar', {
          duration: 5000
        });
      }
    });
  }

  openCreateDialog(): void {
    const dialogRef = this.dialog.open(CreateCardDialogComponent, {
      width: '500px',
      disableClose: true
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.snackBar.open('Cartão criado com sucesso!', 'Fechar', {
          duration: 3000
        });
      }
    });
  }

  openEditDialog(card: Card): void {
    const dialogRef = this.dialog.open(EditCardDialogComponent, {
      width: '500px',
      disableClose: true,
      data: card
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.snackBar.open('Cartão atualizado com sucesso!', 'Fechar', {
          duration: 3000
        });
      }
    });
  }

  openDeleteDialog(card: Card): void {
    const currencyPipe = new CurrencyPipe('pt-BR');

    const dialogRef = this.dialog.open(ConfirmDeleteDialogComponent, {
      width: '400px',
      data: {
        entityName: 'o cartão',
        itemDescription: card.name,
        details: [
          {
            label: 'Banco Emissor',
            value: card.issuingBank
          },
          {
            label: 'Limite',
            value: currencyPipe.transform(card.limit, 'BRL', 'symbol', '1.2-2', 'pt-BR') || ''
          },
          {
            label: 'Dia de Fechamento',
            value: `Dia ${card.closingDay}`
          }
        ],
        warningMessage: 'Todos os lançamentos associados a este cartão também serão excluídos.'
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.cardService.deleteCard(card.id).subscribe({
          next: () => {
            this.snackBar.open('Cartão excluído com sucesso!', 'Fechar', {
              duration: 3000
            });
          },
          error: (error) => {
            console.error('Erro ao excluir cartão:', error);
            this.snackBar.open('Erro ao excluir cartão: ' + error.message, 'Fechar', {
              duration: 5000
            });
          }
        });
      }
    });
  }

  viewTransactions(card: Card): void {
    this.router.navigate(['/cards', card.id, 'transactions']);
  }

  getLimitClass(card: Card): string {
    const percentageUsed = ((card.limit - card.currentLimit) / card.limit) * 100;

    if (percentageUsed >= 90) {
      return 'limit-critical'; // Vermelho - mais de 90% usado
    } else if (percentageUsed >= 70) {
      return 'limit-warning'; // Amarelo - 70-89% usado
    } else if (card.currentLimit < 0) {
      return 'limit-exceeded'; // Vermelho - limite excedido
    } else {
      return 'limit-ok'; // Verde - menos de 70% usado
    }
  }

  applyFilter(event: Event): void {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

  private createFilter(): (card: Card, filter: string) => boolean {
    return (card: Card, filter: string): boolean => {
      const searchStr = filter.toLowerCase();
      return card.name.toLowerCase().includes(searchStr) ||
             card.issuingBank.toLowerCase().includes(searchStr);
    };
  }
}
