import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { tap, map } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';

export enum TransactionType {
  PayableBill = 0,
  ReceivableBill = 1
}

export interface Category {
  id: string;
  name: string;
  color: string;
}

export interface FinancialTransaction {
  id: string;
  description: string;
  amount: number;
  dueDate: Date;
  type: TransactionType;
  createdAt: Date;
  isPaid: boolean;
  categoryId: string;
  category: Category;
  budgetId?: string;
}

export interface CreateFinancialTransactionRequest {
  description: string;
  amount: number;
  dueDate: Date;
  type: TransactionType;
  categoryId: string;
  budgetId?: string;
  isPaid?: boolean;
  isInstallment?: boolean;
  installmentCount?: number;
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
  categoryId: string;
  budgetId?: string;
  isPaid: boolean;
}

export interface PayWithCardRequest {
  transactionIds: string[];
  cardId: string;
  cardAmount: number;
  invoiceYear: number;
  invoiceMonth: number;
}

export interface PayWithCardResponse {
  paidTransactionsCount: number;
  incomeTransactionId: string;
  cardTransactionIds: string[];
}

/**
 * O DueDate é persistido como meia-noite UTC (data pura, sem hora).
 * Por isso os limites do filtro precisam ser montados em UTC a partir do dia
 * escolhido no calendário local — usar `setHours` local deslocaria o início do
 * intervalo em 3h (fuso do Brasil) e esconderia os lançamentos do primeiro dia.
 */
function startOfDayUtc(date: Date): Date {
  const local = new Date(date);
  return new Date(Date.UTC(local.getFullYear(), local.getMonth(), local.getDate(), 0, 0, 0, 0));
}

function endOfDayUtc(date: Date): Date {
  const local = new Date(date);
  return new Date(Date.UTC(local.getFullYear(), local.getMonth(), local.getDate(), 23, 59, 59, 999));
}

function buildDueDateRangeFilter(from: Date, to: Date): string {
  return `(DueDate ge ${startOfDayUtc(from).toISOString()} and DueDate le ${endOfDayUtc(to).toISOString()})`;
}

function buildMonthFilter(month: number, year: number): string {
  const startDate = new Date(Date.UTC(year, month - 1, 1));
  const endDate = new Date(Date.UTC(year, month, 0, 23, 59, 59, 999));
  return `(DueDate ge ${startDate.toISOString()} and DueDate le ${endDate.toISOString()})`;
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

  createTransaction(request: CreateFinancialTransactionRequest): Observable<FinancialTransaction | FinancialTransaction[]> {
    return this.http.post<FinancialTransaction | FinancialTransaction[]>(this.apiUrl, request)
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

        switch (filters.dateRangeType) {
          case 'Today':
            conditions.push(buildDueDateRangeFilter(today, today));
            break;
          case 'ThisWeek': {
            const dayOfWeek = today.getDay();
            const diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1); // adjust when day is sunday
            const firstDayOfWeek = new Date(today);
            firstDayOfWeek.setDate(diff);

            const lastDayOfWeek = new Date(firstDayOfWeek);
            lastDayOfWeek.setDate(firstDayOfWeek.getDate() + 6);

            conditions.push(buildDueDateRangeFilter(firstDayOfWeek, lastDayOfWeek));
            break;
          }
          case 'Month':
            if (filters.month && filters.year) {
              conditions.push(buildMonthFilter(filters.month, filters.year));
            }
            break;
          case 'Custom':
            if (filters.startDate && filters.endDate) {
              conditions.push(buildDueDateRangeFilter(filters.startDate, filters.endDate));
            }
            break;
        }
      } else if (filters.month && filters.year) { // Fallback to old month/year filter if dateRangeType is not set
        conditions.push(buildMonthFilter(filters.month, filters.year));
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
    return this.http.get<any>(url).pipe(
      map(res => Array.isArray(res) ? res : (res.value || []))
    );
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

  deleteTransactions(ids: string[]): Observable<{ deletedCount: number }> {
    return this.http.post<{ deletedCount: number }>(`${this.apiUrl}/delete-batch`, ids)
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

  payWithCard(request: PayWithCardRequest): Observable<PayWithCardResponse> {
    return this.http.post<PayWithCardResponse>(`${this.apiUrl}/pay-with-card`, request)
      .pipe(
        tap(() => this.refreshList.next())
      );
  }
}
