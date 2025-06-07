import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';

export interface Card {
  id: string;
  name: string;
  issuingBank: string;
  closingDay: number;
  limit: number;
  createdAt: Date;
}

export interface CreateCardRequest {
  name: string;
  issuingBank: string;
  closingDay: number;
  limit: number;
}

export interface UpdateCardRequest {
  name: string;
  issuingBank: string;
  closingDay: number;
  limit: number;
}

export interface CardTransaction {
  id: string;
  cardId: string;
  description: string;
  amount: number;
  date: Date;
  installment: string; // Ex: "1/1", "2/12", etc.
  createdAt: Date;
}

export interface CreateCardTransactionRequest {
  cardId: string;
  description: string;
  amount: number;
  date: Date;
  installment: string;
}

export interface UpdateCardTransactionRequest {
  description: string;
  amount: number;
  date: Date;
  installment: string;
}

@Injectable({
  providedIn: 'root'
})
export class CardService {
  private readonly apiUrl = `${environment.apiUrl}/Card`;
  private readonly transactionApiUrl = `${environment.apiUrl}/CardTransaction`;
  private refreshSubject = new Subject<void>();

  public refresh$ = this.refreshSubject.asObservable();

  constructor(private http: HttpClient) { }

  // ============ CARD METHODS ============
  getCards(): Observable<Card[]> {
    return this.http.get<Card[]>(`${this.apiUrl}`);
  }

  getCardById(id: string): Observable<Card> {
    return this.http.get<Card>(`${this.apiUrl}/${id}`);
  }

  createCard(card: CreateCardRequest): Observable<Card> {
    return this.http.post<Card>(`${this.apiUrl}`, card).pipe(
      tap(() => this.refreshSubject.next())
    );
  }

  updateCard(id: string, card: UpdateCardRequest): Observable<Card> {
    return this.http.put<Card>(`${this.apiUrl}/${id}`, card).pipe(
      tap(() => this.refreshSubject.next())
    );
  }

  deleteCard(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      tap(() => this.refreshSubject.next())
    );
  }

  // ============ CARD TRANSACTION METHODS ============
  getTransactionsByCardId(cardId: string): Observable<CardTransaction[]> {
    return this.http.get<CardTransaction[]>(`${this.transactionApiUrl}/card/${cardId}`);
  }

  getTransactionsForInvoice(cardId: string, month: number, year: number): Observable<CardTransaction[]> {
    return this.http.get<CardTransaction[]>(`${this.transactionApiUrl}/card/${cardId}/invoice/${year}/${month}`);
  }

  createCardTransaction(transaction: CreateCardTransactionRequest): Observable<CardTransaction> {
    return this.http.post<CardTransaction>(`${this.transactionApiUrl}`, transaction).pipe(
      tap(() => this.refreshSubject.next())
    );
  }

  updateCardTransaction(id: string, transaction: UpdateCardTransactionRequest): Observable<CardTransaction> {
    return this.http.put<CardTransaction>(`${this.transactionApiUrl}/${id}`, transaction).pipe(
      tap(() => this.refreshSubject.next())
    );
  }

  deleteCardTransaction(id: string): Observable<void> {
    return this.http.delete<void>(`${this.transactionApiUrl}/${id}`).pipe(
      tap(() => this.refreshSubject.next())
    );
  }
}
