import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule, DecimalPipe, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatExpansionModule } from '@angular/material/expansion';
import { Subscription } from 'rxjs';
import { BudgetService, Budget } from '../../services/budget.service';
import { CreateBudgetDialogComponent } from '../create-budget-dialog/create-budget-dialog.component';
import { EditBudgetDialogComponent } from '../edit-budget-dialog/edit-budget-dialog.component';
import { ConfirmDeleteDialogComponent } from '../../../../shared/components/confirm-delete-dialog/confirm-delete-dialog.component';
import { FilterStorageService } from '../../../../shared/services/filter-storage.service';

interface BudgetFilterState {
  selectedMonth: number;
  selectedYear: number;
}

@Component({
  selector: 'app-budget-list',
  templateUrl: './budget-list.component.html',
  styleUrls: ['./budget-list.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatTooltipModule,
    MatMenuModule,
    MatDialogModule,
    MatSnackBarModule,
    MatExpansionModule,
    CurrencyPipe
  ]
})
export class BudgetListComponent implements OnInit, OnDestroy {
  private readonly FILTER_STORAGE_KEY = 'budget-list-filters';

  budgets: Budget[] = [];
  private refreshSubscription: Subscription;

  // Filtros
  selectedMonth: number;
  selectedYear: number;
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

  constructor(
    private budgetService: BudgetService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private filterStorage: FilterStorageService,
    private cdr: ChangeDetectorRef
  ) {
    this.refreshSubscription = this.budgetService.refresh$.subscribe(() => {
      this.loadBudgets();
    });

    const currentDate = new Date();
    this.selectedMonth = currentDate.getMonth() + 1;
    this.selectedYear = currentDate.getFullYear();
    this.currentYear = currentDate.getFullYear();
  }

  ngOnInit(): void {
    this.loadFiltersFromStorage();
    this.loadBudgets();
  }

  ngOnDestroy(): void {
    if (this.refreshSubscription) {
      this.refreshSubscription.unsubscribe();
    }
  }

  loadBudgets(): void {
    this.budgetService.getBudgetsByPeriod(this.selectedMonth, this.selectedYear).subscribe({
      next: (budgets) => {
        this.budgets = budgets;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Erro ao carregar budgets:', error);
        this.snackBar.open('Erro ao carregar budgets: ' + error.message, 'Fechar', {
          duration: 5000
        });
      }
    });
  }

  onPeriodChange(): void {
    this.saveFiltersToStorage();
    this.loadBudgets();
  }

  onMonthChange(month: number): void {
    this.selectedMonth = month;
    this.onPeriodChange();
  }

  onYearChange(year: number): void {
    this.selectedYear = year;
    this.onPeriodChange();
  }

  getProgressColor(usagePercentage: number): string {
    if (usagePercentage < 70) {
      return 'success';
    } else if (usagePercentage <= 90) {
      return 'warning';
    } else {
      return 'danger';
    }
  }

  getProgressBarClass(usagePercentage: number): string {
    if (usagePercentage < 70) {
      return 'progress-green';
    } else if (usagePercentage <= 90) {
      return 'progress-yellow';
    } else {
      return 'progress-red';
    }
  }

  getMonthLabel(month: number): string {
    const monthObj = this.months.find(m => m.value === month);
    return monthObj ? monthObj.label : '';
  }

  openCreateDialog(): void {
    const dialogRef = this.dialog.open(CreateBudgetDialogComponent, {
      width: '500px',
      disableClose: true,
      data: {
        defaultMonth: this.selectedMonth,
        defaultYear: this.selectedYear
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.snackBar.open('Budget criado com sucesso!', 'Fechar', {
          duration: 3000
        });
      }
    });
  }

  openEditDialog(budget: Budget): void {
    const dialogRef = this.dialog.open(EditBudgetDialogComponent, {
      width: '500px',
      disableClose: true,
      data: budget
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.budgetService.updateBudget(budget.id, result).subscribe({
          next: () => {
            this.snackBar.open('Budget atualizado com sucesso!', 'Fechar', {
              duration: 3000
            });
          },
          error: (error) => {
            console.error('Erro ao atualizar budget:', error);
            this.snackBar.open('Erro ao atualizar budget.', 'Fechar', {
              duration: 3000
            });
          }
        });
      }
    });
  }

  openDeleteDialog(budget: Budget): void {
    const details = [];
    let warningMessage: string | undefined;

    if (budget.usedAmount > 0) {
      const currencyPipe = new CurrencyPipe('pt-BR');
      details.push({
        label: 'Transações vinculadas',
        value: currencyPipe.transform(budget.usedAmount, 'BRL', 'symbol', '1.2-2', 'pt-BR') || ''
      });
      warningMessage = 'Este budget possui transações vinculadas.';
    }

    const dialogRef = this.dialog.open(ConfirmDeleteDialogComponent, {
      width: '400px',
      data: {
        entityName: 'Budget',
        itemDescription: budget.name,
        details: details.length > 0 ? details : undefined,
        warningMessage
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.deleteBudget(budget.id);
      }
    });
  }

  private deleteBudget(id: string): void {
    this.budgetService.deleteBudget(id).subscribe({
      next: () => {
        this.snackBar.open('Budget excluído com sucesso!', 'Fechar', {
          duration: 3000
        });
      },
      error: (error) => {
        console.error('Erro ao excluir budget:', error);
        const errorMessage = error.error || 'Erro ao excluir budget.';
        this.snackBar.open(errorMessage, 'Fechar', {
          duration: 5000
        });
      }
    });
  }

  private saveFiltersToStorage(): void {
    this.filterStorage.save<BudgetFilterState>(this.FILTER_STORAGE_KEY, {
      selectedMonth: this.selectedMonth,
      selectedYear: this.selectedYear
    });
  }

  private loadFiltersFromStorage(): void {
    const filterState = this.filterStorage.load<BudgetFilterState>(this.FILTER_STORAGE_KEY);
    if (filterState) {
      this.selectedMonth = filterState.selectedMonth;
      this.selectedYear = filterState.selectedYear;
    }
  }
}

