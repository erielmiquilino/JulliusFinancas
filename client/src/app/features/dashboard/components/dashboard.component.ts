import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule, CurrencyPipe, DecimalPipe } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { forkJoin, Subscription, of } from 'rxjs';
import { debounceTime, catchError, finalize } from 'rxjs/operators';
import { DashboardService } from '../services/dashboard.service';
import { FinancialTransaction, TransactionType } from '../../financial-transaction/services/financial-transaction.service';
import { Budget } from '../../budgets/services/budget.service';
import { FilterStorageService } from '../../../shared/services/filter-storage.service';

interface DashboardFilterState {
  selectedMonth: number;
  selectedYear: number;
}

interface SummaryData {
  totalDespesas: number;
  totalReceitas: number;
  totalDespesasPagas: number;
  totalDespesasEmAberto: number;
  totalReceitasRecebidas: number;
  totalReceitasAReceber: number;
  totalEmConta: number;
  totalPrevistoEmConta: number;
}

interface BudgetSummary {
  totalPlanejado: number;
  totalRealizado: number;
  totalRestante: number;
  percentualUtilizado: number;
  quantidadeBudgets: number;
}

interface ConsolidatedSummary {
  transacoesSemBudget: number;
  budgetsPlanejados: number;
  budgetsExcedentes: number;
  totalGeral: number;
  totalReceitas: number;
  saldoPrevistoGeral: number;
}

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatIconModule,
    MatProgressSpinnerModule,
    CurrencyPipe,
    DecimalPipe
  ]
})
export class DashboardComponent implements OnInit, OnDestroy {
  private readonly FILTER_STORAGE_KEY = 'dashboard-filters';
  private filterSubscription: Subscription | null = null;

  filterForm: FormGroup;
  loading = false;

  private readonly initialSummary: SummaryData = {
    totalDespesas: 0,
    totalReceitas: 0,
    totalDespesasPagas: 0,
    totalDespesasEmAberto: 0,
    totalReceitasRecebidas: 0,
    totalReceitasAReceber: 0,
    totalEmConta: 0,
    totalPrevistoEmConta: 0
  };

  private readonly initialBudgetSummary: BudgetSummary = {
    totalPlanejado: 0,
    totalRealizado: 0,
    totalRestante: 0,
    percentualUtilizado: 0,
    quantidadeBudgets: 0
  };

  private readonly initialConsolidatedSummary: ConsolidatedSummary = {
    transacoesSemBudget: 0,
    budgetsPlanejados: 0,
    budgetsExcedentes: 0,
    totalGeral: 0,
    totalReceitas: 0,
    saldoPrevistoGeral: 0
  };

  summaryData: SummaryData | null = null;
  budgetSummary: BudgetSummary | null = null;
  consolidatedSummary: ConsolidatedSummary | null = null;

  currentMonth: number;
  currentYear: number;
  months = [
    { value: 1, viewValue: 'Janeiro' }, { value: 2, viewValue: 'Fevereiro' }, { value: 3, viewValue: 'Março' },
    { value: 4, viewValue: 'Abril' }, { value: 5, viewValue: 'Maio' }, { value: 6, viewValue: 'Junho' },
    { value: 7, viewValue: 'Julho' }, { value: 8, viewValue: 'Agosto' }, { value: 9, viewValue: 'Setembro' },
    { value: 10, viewValue: 'Outubro' }, { value: 11, viewValue: 'Novembro' }, { value: 12, viewValue: 'Dezembro' }
  ];
  years: number[] = [];

  constructor(
    private fb: FormBuilder,
    private dashboardService: DashboardService,
    private filterStorage: FilterStorageService,
    private cdr: ChangeDetectorRef
  ) {
    const today = new Date();
    this.currentMonth = today.getMonth() + 1;
    this.currentYear = today.getFullYear();

    this.filterForm = this.fb.group({
      month: [this.currentMonth],
      year: [this.currentYear]
    });

    // Popula os anos (ex: últimos 5 anos até o próximo ano)
    for (let i = -2; i <= 5; i++) {
      this.years.push(this.currentYear + i);
    }
  }

  ngOnInit(): void {
    this.loadFiltersFromStorage();
    this.loadSummary();
    this.filterSubscription = this.filterForm.valueChanges
      .pipe(debounceTime(300))
      .subscribe(() => {
        this.saveFiltersToStorage();
        this.loadSummary();
      });
  }

  ngOnDestroy(): void {
    if (this.filterSubscription) {
      this.filterSubscription.unsubscribe();
    }
  }

  loadSummary(): void {
    const { month, year } = this.filterForm.value;
    if (!month || !year) return;

    this.loading = true;

    // Busca transações e budgets em paralelo
    forkJoin({
      transactions: this.dashboardService.getTransactions({ month, year }).pipe(
        catchError(err => {
          console.error('Erro ao buscar transações:', err);
          return of([]);
        })
      ),
      budgets: this.dashboardService.getBudgetsByPeriod(month, year).pipe(
        catchError(err => {
          console.error('Erro ao buscar budgets:', err);
          return of([]);
        })
      )
    })
    .pipe(
      finalize(() => {
        this.loading = false;
        this.cdr.detectChanges();
      })
    )
    .subscribe({
      next: ({ transactions, budgets }) => {
        this.calculateSummary(transactions);
        this.calculateBudgetSummary(budgets);
        this.calculateConsolidatedSummary(transactions, budgets);
        this.cdr.detectChanges();
      },
      error: (error: any) => {
        console.error('Erro ao processar dados:', error);
        this.summaryData = { ...this.initialSummary };
        this.budgetSummary = { ...this.initialBudgetSummary };
        this.consolidatedSummary = { ...this.initialConsolidatedSummary };
        this.cdr.detectChanges();
      }
    });
  }

  calculateSummary(transactions: FinancialTransaction[]): void {
    let totalDespesas = 0;
    let totalReceitas = 0;
    let totalDespesasPagas = 0;
    let totalDespesasEmAberto = 0;
    let totalReceitasRecebidas = 0;
    let totalReceitasAReceber = 0;

    transactions.forEach(transaction => {
      if (transaction.type === TransactionType.PayableBill) {
        totalDespesas += transaction.amount;
        if (transaction.isPaid) {
          totalDespesasPagas += transaction.amount;
        } else {
          totalDespesasEmAberto += transaction.amount;
        }
      } else if (transaction.type === TransactionType.ReceivableBill) {
        totalReceitas += transaction.amount;
        if (transaction.isPaid) {
          totalReceitasRecebidas += transaction.amount;
        } else {
          totalReceitasAReceber += transaction.amount;
        }
      }
    });

    const totalEmConta = totalReceitasRecebidas - totalDespesasPagas;
    const totalPrevistoEmConta = totalReceitas - totalDespesas;

    this.summaryData = {
      totalDespesas,
      totalReceitas,
      totalDespesasPagas,
      totalDespesasEmAberto,
      totalReceitasRecebidas,
      totalReceitasAReceber,
      totalEmConta,
      totalPrevistoEmConta
    };
  }

  calculateBudgetSummary(budgets: Budget[]): void {
    if (!budgets || budgets.length === 0) {
      this.budgetSummary = {
        totalPlanejado: 0,
        totalRealizado: 0,
        totalRestante: 0,
        percentualUtilizado: 0,
        quantidadeBudgets: 0
      };
      return;
    }

    const totalPlanejado = budgets.reduce((sum, b) => sum + b.limitAmount, 0);
    const totalRealizado = budgets.reduce((sum, b) => sum + b.usedAmount, 0);
    const totalRestante = totalPlanejado - totalRealizado;
    const percentualUtilizado = totalPlanejado > 0 ? (totalRealizado / totalPlanejado) * 100 : 0;

    this.budgetSummary = {
      totalPlanejado,
      totalRealizado,
      totalRestante,
      percentualUtilizado,
      quantidadeBudgets: budgets.length
    };
  }

  calculateConsolidatedSummary(transactions: FinancialTransaction[], budgets: Budget[]): void {
    // Transacoes SEM budget vinculado (evita duplicacao)
    const transacoesSemBudget = transactions
      .filter(t => t.type === TransactionType.PayableBill && !t.budgetId)
      .reduce((sum, t) => sum + t.amount, 0);

    // Total planejado nos budgets
    const budgetsPlanejados = budgets.reduce((sum, b) => sum + b.limitAmount, 0);

    // Total excedente dos budgets (quando usedAmount > limitAmount)
    const budgetsExcedentes = budgets.reduce((sum, b) => {
      const excedente = b.usedAmount - b.limitAmount;
      return sum + (excedente > 0 ? excedente : 0);
    }, 0);

    // Total geral (transacoes sem budget + budgets planejados + excedentes)
    const totalGeral = transacoesSemBudget + budgetsPlanejados + budgetsExcedentes;

    // Total de receitas
    const totalReceitas = transactions
      .filter(t => t.type === TransactionType.ReceivableBill)
      .reduce((sum, t) => sum + t.amount, 0);

    // Saldo previsto (receitas - total geral)
    const saldoPrevistoGeral = totalReceitas - totalGeral;

    this.consolidatedSummary = {
      transacoesSemBudget,
      budgetsPlanejados,
      budgetsExcedentes,
      totalGeral,
      totalReceitas,
      saldoPrevistoGeral
    };
  }

  getBudgetProgressClass(percentage: number): string {
    if (percentage < 70) return 'progress-green';
    if (percentage <= 90) return 'progress-yellow';
    return 'progress-red';
  }

  private saveFiltersToStorage(): void {
    this.filterStorage.save<DashboardFilterState>(this.FILTER_STORAGE_KEY, {
      selectedMonth: this.filterForm.value.month,
      selectedYear: this.filterForm.value.year
    });
  }

  private loadFiltersFromStorage(): void {
    const filterState = this.filterStorage.load<DashboardFilterState>(this.FILTER_STORAGE_KEY);
    if (filterState) {
      this.filterForm.patchValue({
        month: filterState.selectedMonth,
        year: filterState.selectedYear
      }, { emitEvent: false });
    }
  }
}
