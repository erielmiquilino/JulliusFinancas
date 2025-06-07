import { Component, OnInit, OnDestroy, ViewChild, AfterViewInit, ChangeDetectorRef } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { CardService, Card } from '../../services/card.service';
import { CreateCardDialogComponent } from '../create-card-dialog/create-card-dialog.component';
import { EditCardDialogComponent } from '../edit-card-dialog/edit-card-dialog.component';
import { DeleteCardDialogComponent } from '../delete-card-dialog/delete-card-dialog.component';

@Component({
  selector: 'app-card-list',
  templateUrl: './card-list.component.html',
  styleUrls: ['./card-list.component.scss']
})
export class CardListComponent implements OnInit, OnDestroy, AfterViewInit {
  displayedColumns: string[] = ['name', 'issuingBank', 'closingDay', 'dueDay', 'limit', 'actions'];
  dataSource: MatTableDataSource<Card>;
  private refreshSubscription: Subscription;

  @ViewChild(MatSort) sort!: MatSort;

  constructor(
    private cardService: CardService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {
    this.dataSource = new MatTableDataSource<Card>();
    this.refreshSubscription = this.cardService.refresh$.subscribe(() => {
      this.loadCards();
    });
  }

  ngOnInit(): void {
    this.loadCards();
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
    const dialogRef = this.dialog.open(DeleteCardDialogComponent, {
      width: '400px',
      data: card
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
}
