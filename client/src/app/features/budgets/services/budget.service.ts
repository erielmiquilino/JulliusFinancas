import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';

export interface Budget {
  id: string;
  name: string;
  limitAmount: number;
  description?: string;
  month: number;
  year: number;
  createdAt: Date;
  usedAmount: number;
  remainingAmount: number;
  usagePercentage: number;
}

export interface CreateBudgetRequest {
  name: string;
  limitAmount: number;
  description?: string;
  month: number;
  year: number;
}

export interface UpdateBudgetRequest {
  name: string;
  limitAmount: number;
  description?: string;
  month: number;
  year: number;
}

export interface BudgetFilters {
  month?: number;
  year?: number;
}

@Injectable({
  providedIn: 'root'
})
export class BudgetService {
  private apiUrl = `${environment.apiUrl}/Budget`;
  private refreshList = new Subject<void>();

  constructor(private http: HttpClient) { }

  get refresh$() {
    return this.refreshList.asObservable();
  }

  createBudget(request: CreateBudgetRequest): Observable<Budget> {
    return this.http.post<Budget>(this.apiUrl, request)
      .pipe(
        tap(() => this.refreshList.next())
      );
  }

  getAllBudgets(): Observable<Budget[]> {
    return this.http.get<Budget[]>(this.apiUrl);
  }

  getBudgetsByPeriod(month: number, year: number): Observable<Budget[]> {
    return this.http.get<Budget[]>(`${this.apiUrl}/by-period?month=${month}&year=${year}`);
  }

  getBudgetById(id: string): Observable<Budget> {
    return this.http.get<Budget>(`${this.apiUrl}/${id}`);
  }

  updateBudget(id: string, request: UpdateBudgetRequest): Observable<Budget> {
    return this.http.put<Budget>(`${this.apiUrl}/${id}`, request)
      .pipe(
        tap(() => this.refreshList.next())
      );
  }

  deleteBudget(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`)
      .pipe(
        tap(() => this.refreshList.next())
      );
  }

  triggerRefresh(): void {
    this.refreshList.next();
  }
}

