import { Component, OnInit, OnDestroy, ViewChild, AfterViewInit } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatPaginator } from '@angular/material/paginator';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { CardService, Card } from '../../services/card.service';
import { CreateCardDialogComponent } from '../create-card-dialog/create-card-dialog.component';

@Component({
  selector: 'app-card-list',
  templateUrl: './card-list.component.html',
  styleUrls: ['./card-list.component.scss']
})
export class CardListComponent implements OnInit, OnDestroy, AfterViewInit {
  displayedColumns: string[] = ['nome', 'bancoEmissor', 'bandeira', 'final', 'limite', 'actions'];
  dataSource: MatTableDataSource<Card>;
  private refreshSubscription: Subscription;

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  constructor(
    private cardService: CardService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private router: Router
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
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
    if (this.sort) {
      this.sort.active = 'nome';
      this.sort.direction = 'asc';
    }
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
        this.dataSource.paginator = this.paginator;
        this.dataSource.sort = this.sort;

        if (this.sort && !this.sort.active) {
          this.sort.active = 'nome';
          this.sort.direction = 'asc';
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

  viewTransactions(card: Card): void {
    this.router.navigate(['/cards', card.id, 'transactions']);
  }
}
