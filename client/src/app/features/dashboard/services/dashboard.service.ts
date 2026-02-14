import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import { FinancialTransaction, TransactionFilters, TransactionType } from '../../financial-transaction/services/financial-transaction.service';
import { Budget } from '../../budgets/services/budget.service';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private apiUrl = `${environment.apiUrl}/FinancialTransaction`;
  private budgetApiUrl = `${environment.apiUrl}/Budget`;

  constructor(private http: HttpClient) { }

  getTransactions(filters?: TransactionFilters): Observable<FinancialTransaction[]> {
    let filterString = '';
    const queryParams: string[] = [];

    queryParams.push('$top=1000');

    if (filters) {
      const conditions: string[] = [];

      if (filters.month && filters.year) {
        const startDate = new Date(Date.UTC(filters.year, filters.month - 1, 1));
        const endDate = new Date(Date.UTC(filters.year, filters.month, 0, 23, 59, 59, 999)); // Final do dia em UTC
        conditions.push(`(DueDate ge ${startDate.toISOString()} and DueDate le ${endDate.toISOString()})`);
      } else if (filters.year) { // Adicionado para filtrar apenas por ano se o mês não for fornecido
        const startDate = new Date(Date.UTC(filters.year, 0, 1));
        const endDate = new Date(Date.UTC(filters.year, 11, 31, 23, 59, 59, 999));
        conditions.push(`(DueDate ge ${startDate.toISOString()} and DueDate le ${endDate.toISOString()})`);
      }


      if (filters.type !== undefined && filters.type !== null) {
        conditions.push(`Type eq ${filters.type}`); // OData enum representation
      }

      if (conditions.length > 0) {
        queryParams.push(`$filter=${conditions.join(' and ')}`);
      }
    }

    const url = queryParams.length > 0 ? `${this.apiUrl}?${queryParams.join('&')}` : this.apiUrl;
    return this.http.get<any>(url).pipe(
      map(res => Array.isArray(res) ? res : (res.value || []))
    );
  }

  getBudgetsByPeriod(month: number, year: number): Observable<Budget[]> {
    return this.http.get<Budget[]>(`${this.budgetApiUrl}/by-period?month=${month}&year=${year}`);
  }
}
