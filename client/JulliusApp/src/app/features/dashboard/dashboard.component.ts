import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { DashboardService } from './services/dashboard.service';
import { FinancialTransaction, TransactionType } from '../financial-transaction/services/financial-transaction.service';

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

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  filterForm: FormGroup;
  summaryData: SummaryData | null = null;
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
    private dashboardService: DashboardService
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
    this.loadSummary();
    this.filterForm.valueChanges.subscribe(() => {
      this.loadSummary();
    });
  }

  loadSummary(): void {
    const { month, year } = this.filterForm.value;
    if (!month || !year) return;

    this.dashboardService.getTransactions({ month, year }).subscribe(transactions => {
      this.calculateSummary(transactions);
    }, error => {
      console.error('Erro ao buscar transações:', error);
      this.summaryData = null; // Reseta em caso de erro
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
}
