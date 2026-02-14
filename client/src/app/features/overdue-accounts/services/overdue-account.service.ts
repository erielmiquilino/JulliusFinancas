import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';

export interface OverdueAccount {
  id: string;
  description: string;
  currentDebtValue: number;
  createdAt: Date;
}

export interface CreateOverdueAccountRequest {
  description: string;
  currentDebtValue: number;
}

export interface UpdateOverdueAccountRequest {
  description: string;
  currentDebtValue: number;
}

@Injectable({
  providedIn: 'root'
})
export class OverdueAccountService {
  private apiUrl = `${environment.apiUrl}/OverdueAccount`;
  private refreshList = new Subject<void>();

  constructor(private http: HttpClient) { }

  get refresh$() {
    return this.refreshList.asObservable();
  }

  createOverdueAccount(request: CreateOverdueAccountRequest): Observable<OverdueAccount> {
    return this.http.post<OverdueAccount>(this.apiUrl, request)
      .pipe(
        tap(() => this.refreshList.next())
      );
  }

  getAllOverdueAccounts(): Observable<OverdueAccount[]> {
    return this.http.get<OverdueAccount[]>(this.apiUrl);
  }

  getOverdueAccountById(id: string): Observable<OverdueAccount> {
    return this.http.get<OverdueAccount>(`${this.apiUrl}/${id}`);
  }

  updateOverdueAccount(id: string, request: UpdateOverdueAccountRequest): Observable<OverdueAccount> {
    return this.http.put<OverdueAccount>(`${this.apiUrl}/${id}`, request)
      .pipe(
        tap(() => this.refreshList.next())
      );
  }

  deleteOverdueAccount(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`)
      .pipe(
        tap(() => this.refreshList.next())
      );
  }

  triggerRefresh(): void {
    this.refreshList.next();
  }
}
