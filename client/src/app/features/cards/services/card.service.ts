import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, Subject, of } from 'rxjs';
import { tap, delay } from 'rxjs/operators';
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
  private refreshSubject = new Subject<void>();

  public refresh$ = this.refreshSubject.asObservable();



  private mockTransactions: CardTransaction[] = [
    // Transações do Cartão 1 - Janeiro 2024
    {
      id: '1',
      cardId: '1',
      description: 'Supermercado Extra',
      amount: 150.75,
      date: new Date('2024-01-15'),
      installment: '1/1',
      createdAt: new Date('2024-01-15')
    },
    {
      id: '2',
      cardId: '1',
      description: 'Netflix Assinatura',
      amount: 29.90,
      date: new Date('2024-01-10'),
      installment: '1/1',
      createdAt: new Date('2024-01-10')
    },
    {
      id: '3',
      cardId: '1',
      description: 'Combustível Posto Shell',
      amount: 85.40,
      date: new Date('2024-01-20'),
      installment: '1/1',
      createdAt: new Date('2024-01-20')
    },

    // Transações do Cartão 1 - Fevereiro 2024
    {
      id: '8',
      cardId: '1',
      description: 'Supermercado Carrefour',
      amount: 180.30,
      date: new Date('2024-02-08'),
      installment: '1/1',
      createdAt: new Date('2024-02-08')
    },
    {
      id: '9',
      cardId: '1',
      description: 'Netflix Assinatura',
      amount: 29.90,
      date: new Date('2024-02-10'),
      installment: '1/1',
      createdAt: new Date('2024-02-10')
    },

    // Transações do Cartão 1 - Dezembro 2024 (atual)
    {
      id: '10',
      cardId: '1',
      description: 'Compras de Natal',
      amount: 450.80,
      date: new Date('2024-12-15'),
      installment: '1/1',
      createdAt: new Date('2024-12-15')
    },
    {
      id: '11',
      cardId: '1',
      description: 'Netflix Assinatura',
      amount: 29.90,
      date: new Date('2024-12-10'),
      installment: '1/1',
      createdAt: new Date('2024-12-10')
    },

    // Transações do Cartão 2 - Janeiro 2024
    {
      id: '4',
      cardId: '2',
      description: 'Notebook Dell',
      amount: 2500.00,
      date: new Date('2024-01-12'),
      installment: '1/12',
      createdAt: new Date('2024-01-12')
    },

    // Transações do Cartão 2 - Fevereiro 2024
    {
      id: '5',
      cardId: '2',
      description: 'Notebook Dell',
      amount: 2500.00,
      date: new Date('2024-02-12'),
      installment: '2/12',
      createdAt: new Date('2024-01-12')
    },

    // Transações do Cartão 2 - Dezembro 2024
    {
      id: '12',
      cardId: '2',
      description: 'Notebook Dell',
      amount: 2500.00,
      date: new Date('2024-12-12'),
      installment: '12/12',
      createdAt: new Date('2024-01-12')
    },
    {
      id: '13',
      cardId: '2',
      description: 'Mouse Logitech',
      amount: 150.00,
      date: new Date('2024-12-05'),
      installment: '1/1',
      createdAt: new Date('2024-12-05')
    },

    // Transações do Cartão 3 - Janeiro 2024
    {
      id: '6',
      cardId: '3',
      description: 'Restaurante Outback',
      amount: 120.00,
      date: new Date('2024-01-13'),
      installment: '1/2',
      createdAt: new Date('2024-01-13')
    },

    // Transações do Cartão 3 - Fevereiro 2024
    {
      id: '7',
      cardId: '3',
      description: 'Restaurante Outback',
      amount: 120.00,
      date: new Date('2024-02-13'),
      installment: '2/2',
      createdAt: new Date('2024-01-13')
    },

    // Transações do Cartão 3 - Dezembro 2024
    {
      id: '14',
      cardId: '3',
      description: 'Ceia de Natal',
      amount: 280.50,
      date: new Date('2024-12-24'),
      installment: '1/1',
      createdAt: new Date('2024-12-24')
    }
  ];

  constructor(private http: HttpClient) { }

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

  getTransactionsByCardId(cardId: string): Observable<CardTransaction[]> {
    const transactions = this.mockTransactions.filter(t => t.cardId === cardId);
    return of([...transactions]).pipe(delay(300));
  }

  getTransactionsForInvoice(cardId: string, month: number, year: number): Observable<CardTransaction[]> {
    const transactions = this.mockTransactions.filter(t => {
      if (t.cardId !== cardId) return false;

      const transactionDate = new Date(t.date);
      const transactionMonth = transactionDate.getMonth() + 1; // getMonth() retorna 0-11
      const transactionYear = transactionDate.getFullYear();

      return transactionMonth === month && transactionYear === year;
    });

    return of([...transactions]).pipe(delay(300));
  }

  createCardTransaction(transaction: CreateCardTransactionRequest): Observable<CardTransaction> {
    const newTransaction: CardTransaction = {
      ...transaction,
      id: (this.mockTransactions.length + 1).toString(),
      createdAt: new Date()
    };
    this.mockTransactions.push(newTransaction);
    return of(newTransaction).pipe(
      delay(300),
      tap(() => this.refreshSubject.next())
    );
  }

  updateCardTransaction(id: string, transaction: UpdateCardTransactionRequest): Observable<CardTransaction> {
    const transactionIndex = this.mockTransactions.findIndex(t => t.id === id);
    if (transactionIndex === -1) {
      throw new Error(`Transação com ID ${id} não encontrada`);
    }

    this.mockTransactions[transactionIndex] = {
      ...this.mockTransactions[transactionIndex],
      ...transaction,
      id: id
    };

    return of(this.mockTransactions[transactionIndex]).pipe(
      delay(300),
      tap(() => this.refreshSubject.next())
    );
  }

  deleteCardTransaction(id: string): Observable<void> {
    const transactionIndex = this.mockTransactions.findIndex(t => t.id === id);
    if (transactionIndex === -1) {
      throw new Error(`Transação com ID ${id} não encontrada`);
    }

    this.mockTransactions.splice(transactionIndex, 1);
    return of(void 0).pipe(
      delay(300),
      tap(() => this.refreshSubject.next())
    );
  }
}
