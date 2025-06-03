import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';

export enum TransactionType {
  PayableBill = 0,
  ReceivableBill = 1
}

export interface FinancialTransaction {
  id: string;
  description: string;
  amount: number;
  dueDate: Date;
  type: TransactionType;
  createdAt: Date;
  isPaid: boolean;
}

export interface CreateFinancialTransactionRequest {
  description: string;
  amount: number;
  dueDate: Date;
  type: TransactionType;
}

export interface TransactionFilters {
  month?: number;
  year?: number;
  type?: TransactionType;
  dateRangeType?: 'Today' | 'ThisWeek' | 'Month' | 'Custom';
  startDate?: Date;
  endDate?: Date;
  paymentStatus?: 'Paid' | 'Pending' | 'All';
}

export interface UpdateFinancialTransactionRequest {
  description: string;
  amount: number;
  dueDate: Date;
  type: TransactionType;
}

@Injectable({
  providedIn: 'root'
})
export class FinancialTransactionService {
  private apiUrl = `${environment.apiUrl}/FinancialTransaction`;
  private refreshList = new Subject<void>();

  constructor(private http: HttpClient) { }

  get refresh$() {
    return this.refreshList.asObservable();
  }

  createTransaction(request: CreateFinancialTransactionRequest): Observable<FinancialTransaction> {
    return this.http.post<FinancialTransaction>(this.apiUrl, request)
      .pipe(
        tap(() => this.refreshList.next())
      );
  }

  getAllTransactions(filters?: TransactionFilters): Observable<FinancialTransaction[]> {
    let filterString = '';

    if (filters) {
      const conditions: string[] = [];

      if (filters.dateRangeType) {
        const today = new Date();
        today.setHours(0, 0, 0, 0);

        switch (filters.dateRangeType) {
          case 'Today':
            const tomorrow = new Date(today);
            tomorrow.setDate(today.getDate() + 1);
            conditions.push(`(DueDate ge ${today.toISOString()} and DueDate lt ${tomorrow.toISOString()})`);
            break;
          case 'ThisWeek':
            const firstDayOfWeek = new Date(today);
            const dayOfWeek = today.getDay();
            const diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1); // adjust when day is sunday
            firstDayOfWeek.setDate(diff);

            const lastDayOfWeek = new Date(firstDayOfWeek);
            lastDayOfWeek.setDate(firstDayOfWeek.getDate() + 6);
            lastDayOfWeek.setHours(23, 59, 59, 999);

            conditions.push(`(DueDate ge ${firstDayOfWeek.toISOString()} and DueDate le ${lastDayOfWeek.toISOString()})`);
            break;
          case 'Month':
            if (filters.month && filters.year) {
              const startDate = new Date(Date.UTC(filters.year, filters.month - 1, 1));
              const endDate = new Date(filters.year, filters.month, 0, 23, 59, 59, 999);
              conditions.push(`(DueDate ge ${startDate.toISOString()} and DueDate le ${endDate.toISOString()})`);
            }
            break;
          case 'Custom':
            if (filters.startDate && filters.endDate) {
              const startDate = new Date(filters.startDate);
              startDate.setHours(0, 0, 0, 0);
              const endDate = new Date(filters.endDate);
              endDate.setHours(23, 59, 59, 999);
              conditions.push(`(DueDate ge ${startDate.toISOString()} and DueDate le ${endDate.toISOString()})`);
            }
            break;
        }
      } else if (filters.month && filters.year) { // Fallback to old month/year filter if dateRangeType is not set
        const startDate = new Date(Date.UTC(filters.year, filters.month - 1, 1));
        const endDate = new Date(filters.year, filters.month, 0, 23, 59, 59, 999);
        conditions.push(`(DueDate ge ${startDate.toISOString()} and DueDate le ${endDate.toISOString()})`);
      }

      if (filters.type !== undefined) {
        conditions.push(`Type eq '${TransactionType[filters.type]}'`);
      }

      if (filters.paymentStatus && filters.paymentStatus !== 'All') {
        conditions.push(`IsPaid eq ${filters.paymentStatus === 'Paid' ? 'true' : 'false'}`);
      }

      if (conditions.length > 0) {
        filterString = `$filter=${conditions.join(' and ')}`;
      }
    }

    const url = filterString ? `${this.apiUrl}?${filterString}` : this.apiUrl;
    return this.http.get<FinancialTransaction[]>(url);
  }

  getTransactionById(id: string): Observable<FinancialTransaction> {
    return this.http.get<FinancialTransaction>(`${this.apiUrl}/${id}`);
  }

  deleteTransaction(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`)
      .pipe(
        tap(() => this.refreshList.next())
      );
  }

  updateTransaction(id: string, request: UpdateFinancialTransactionRequest): Observable<FinancialTransaction> {
    return this.http.put<FinancialTransaction>(`${this.apiUrl}/${id}`, request)
      .pipe(
        tap(() => this.refreshList.next())
      );
  }

  updatePaymentStatus(id: string, isPaid: boolean): Observable<FinancialTransaction> {
    return this.http.patch<FinancialTransaction>(`${this.apiUrl}/${id}/payment-status`, isPaid)
      .pipe(
        tap(() => this.refreshList.next())
      );
  }
}
