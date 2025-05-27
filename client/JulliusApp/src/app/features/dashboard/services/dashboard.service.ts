import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { FinancialTransaction, TransactionFilters, TransactionType } from '../../financial-transaction/services/financial-transaction.service'; // Ajuste o caminho se necessário

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private apiUrl = `${environment.apiUrl}/api/FinancialTransaction`;

  constructor(private http: HttpClient) { }

  getTransactions(filters?: TransactionFilters): Observable<FinancialTransaction[]> {
    let filterString = '';

    if (filters) {
      const conditions: string[] = [];

      if (filters.month && filters.year) {
        const startDate = new Date(filters.year, filters.month - 1, 1);
        const endDate = new Date(filters.year, filters.month, 0, 23, 59, 59, 999); // Final do dia
        conditions.push(`(DueDate ge ${startDate.toISOString()} and DueDate le ${endDate.toISOString()})`);
      } else if (filters.year) { // Adicionado para filtrar apenas por ano se o mês não for fornecido
        const startDate = new Date(filters.year, 0, 1);
        const endDate = new Date(filters.year, 11, 31, 23, 59, 59, 999);
        conditions.push(`(DueDate ge ${startDate.toISOString()} and DueDate le ${endDate.toISOString()})`);
      }


      if (filters.type !== undefined && filters.type !== null) {
        conditions.push(`Type eq ${filters.type}`); // OData enum representation
      }

      if (conditions.length > 0) {
        filterString = `$filter=${conditions.join(' and ')}`;
      }
    }

    const url = filterString ? `${this.apiUrl}?${filterString}` : this.apiUrl;
    return this.http.get<FinancialTransaction[]>(url);
  }
}
