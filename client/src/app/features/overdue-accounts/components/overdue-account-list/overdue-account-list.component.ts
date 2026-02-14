import { Component, OnInit, OnDestroy, ViewChild, ChangeDetectorRef } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';
import { MatExpansionModule } from '@angular/material/expansion';
import { BreakpointObserver } from '@angular/cdk/layout';
import { Subscription, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { OverdueAccountService, OverdueAccount } from '../../services/overdue-account.service';
import { CreateOverdueAccountDialogComponent } from '../create-overdue-account-dialog/create-overdue-account-dialog.component';
import { EditOverdueAccountDialogComponent } from '../edit-overdue-account-dialog/edit-overdue-account-dialog.component';
import { ConfirmDeleteDialogComponent } from '../../../../shared/components/confirm-delete-dialog/confirm-delete-dialog.component';

@Component({
  selector: 'app-overdue-account-list',
  templateUrl: './overdue-account-list.component.html',
  styleUrls: ['./overdue-account-list.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatSortModule,
    MatDialogModule,
    MatSnackBarModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatTooltipModule,
    MatMenuModule,
    MatExpansionModule,
    CurrencyPipe,
    DatePipe
  ]
})
export class OverdueAccountListComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private readonly desktopColumns: string[] = ['description', 'currentDebtValue', 'createdAt', 'actions'];
  private readonly mobileColumns: string[] = ['description', 'currentDebtValue', 'actions'];

  displayedColumns: string[] = this.desktopColumns;
  dataSource: MatTableDataSource<OverdueAccount>;
  textFilter: string = '';
  totalAmount: number = 0;
  isMobile = false;

  @ViewChild(MatSort) sort!: MatSort;

  private refreshSubscription: Subscription;

  constructor(
    private overdueAccountService: OverdueAccountService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private breakpointObserver: BreakpointObserver,
    private cdr: ChangeDetectorRef
  ) {
    this.dataSource = new MatTableDataSource<OverdueAccount>();
    this.refreshSubscription = this.overdueAccountService.refresh$.subscribe(() => {
      this.loadOverdueAccounts();
    });
  }

  ngOnInit(): void {
    this.loadOverdueAccounts();
    this.setupFilter();

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

  ngAfterViewInit(): void {
    this.dataSource.sort = this.sort;
  }

  private setupFilter(): void {
    this.dataSource.filterPredicate = (data: OverdueAccount, filter: string) => {
      const normalized = filter.trim().toLowerCase();
      return data.description?.toLowerCase().includes(normalized) ?? false;
    };
  }

  loadOverdueAccounts(): void {
    this.overdueAccountService.getAllOverdueAccounts().subscribe({
      next: (overdueAccounts) => {
        this.dataSource.data = overdueAccounts;
        this.calculateTotal();
      },
      error: (error) => {
        console.error('Erro ao carregar contas atrasadas:', error);
        this.snackBar.open('Erro ao carregar contas atrasadas: ' + error.message, 'Fechar', {
          duration: 5000
        });
      }
    });
  }

  private calculateTotal(): void {
    this.totalAmount = this.dataSource.data.reduce((sum, account) => sum + account.currentDebtValue, 0);
  }

  applyFilter(event: Event): void {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

  openCreateDialog(): void {
    const dialogRef = this.dialog.open(CreateOverdueAccountDialogComponent, {
      width: '500px',
      disableClose: true
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.snackBar.open('Conta atrasada criada com sucesso!', 'Fechar', {
          duration: 3000
        });
      }
    });
  }

  openEditDialog(overdueAccount: OverdueAccount): void {
    const dialogRef = this.dialog.open(EditOverdueAccountDialogComponent, {
      width: '500px',
      disableClose: true,
      data: overdueAccount
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.overdueAccountService.updateOverdueAccount(overdueAccount.id, result).subscribe({
          next: () => {
            this.snackBar.open('Conta atrasada atualizada com sucesso!', 'Fechar', {
              duration: 3000
            });
          },
          error: (error) => {
            console.error('Erro ao atualizar conta atrasada:', error);
            this.snackBar.open('Erro ao atualizar conta atrasada.', 'Fechar', {
              duration: 3000
            });
          }
        });
      }
    });
  }

  openDeleteDialog(overdueAccount: OverdueAccount): void {
    const currencyPipe = new CurrencyPipe('pt-BR');

    const dialogRef = this.dialog.open(ConfirmDeleteDialogComponent, {
      width: '400px',
      data: {
        entityName: 'a conta atrasada',
        itemDescription: overdueAccount.description,
        details: [{
          label: 'Valor',
          value: currencyPipe.transform(overdueAccount.currentDebtValue, 'BRL', 'symbol', '1.2-2', 'pt-BR') || ''
        }]
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.deleteOverdueAccount(overdueAccount.id);
      }
    });
  }

  private deleteOverdueAccount(id: string): void {
    this.overdueAccountService.deleteOverdueAccount(id).subscribe({
      next: () => {
        this.snackBar.open('Conta atrasada excluída com sucesso!', 'Fechar', {
          duration: 3000
        });
      },
      error: (error) => {
        console.error('Erro ao excluir conta atrasada:', error);
        const errorMessage = error.error || 'Erro ao excluir conta atrasada.';
        this.snackBar.open(errorMessage, 'Fechar', {
          duration: 5000
        });
      }
    });
  }
}
