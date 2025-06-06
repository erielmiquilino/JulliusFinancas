import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, Subject, of } from 'rxjs';
import { tap, delay } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';

export interface Card {
  id: string;
  nome: string;
  bancoEmissor: string;
  bandeira: string;
  diaFechamento: number;
  limite: number;
  createdAt: Date;
}

export interface CreateCardRequest {
  nome: string;
  bancoEmissor: string;
  bandeira: string;
  diaFechamento: number;
  limite: number;
}

export interface UpdateCardRequest {
  nome: string;
  bancoEmissor: string;
  bandeira: string;
  diaFechamento: number;
  limite: number;
}

export interface CardTransaction {
  id: string;
  cardId: string;
  descricao: string;
  valor: number;
  data: Date;
  parcela: string; // Ex: "1/1", "2/12", etc.
  createdAt: Date;
}

export interface CreateCardTransactionRequest {
  cardId: string;
  descricao: string;
  valor: number;
  data: Date;
  parcela: string;
}

export interface UpdateCardTransactionRequest {
  descricao: string;
  valor: number;
  data: Date;
  parcela: string;
}

@Injectable({
  providedIn: 'root'
})
export class CardService {
  private readonly apiUrl = `${environment.apiUrl}/cards`;
  private refreshSubject = new Subject<void>();

  public refresh$ = this.refreshSubject.asObservable();

  // Dados mockados para teste
  private mockCards: Card[] = [
    {
      id: '1',
      nome: 'Cartão Principal',
      bancoEmissor: 'Banco do Brasil',
      bandeira: 'Visa',
      diaFechamento: 15,
      limite: 5000.00,
      createdAt: new Date('2024-01-01')
    },
    {
      id: '2',
      nome: 'Cartão Corporativo',
      bancoEmissor: 'Itaú',
      bandeira: 'Mastercard',
      diaFechamento: 10,
      limite: 15000.00,
      createdAt: new Date('2024-01-05')
    },
    {
      id: '3',
      nome: 'Cartão Reserva',
      bancoEmissor: 'Santander',
      bandeira: 'Elo',
      diaFechamento: 25,
      limite: 2000.00,
      createdAt: new Date('2024-01-10')
    }
  ];

  private mockTransactions: CardTransaction[] = [
    // Transações do Cartão 1 - Janeiro 2024
    {
      id: '1',
      cardId: '1',
      descricao: 'Supermercado Extra',
      valor: 150.75,
      data: new Date('2024-01-15'),
      parcela: '1/1',
      createdAt: new Date('2024-01-15')
    },
    {
      id: '2',
      cardId: '1',
      descricao: 'Netflix Assinatura',
      valor: 29.90,
      data: new Date('2024-01-10'),
      parcela: '1/1',
      createdAt: new Date('2024-01-10')
    },
    {
      id: '3',
      cardId: '1',
      descricao: 'Combustível Posto Shell',
      valor: 85.40,
      data: new Date('2024-01-20'),
      parcela: '1/1',
      createdAt: new Date('2024-01-20')
    },

    // Transações do Cartão 1 - Fevereiro 2024
    {
      id: '8',
      cardId: '1',
      descricao: 'Supermercado Carrefour',
      valor: 180.30,
      data: new Date('2024-02-08'),
      parcela: '1/1',
      createdAt: new Date('2024-02-08')
    },
    {
      id: '9',
      cardId: '1',
      descricao: 'Netflix Assinatura',
      valor: 29.90,
      data: new Date('2024-02-10'),
      parcela: '1/1',
      createdAt: new Date('2024-02-10')
    },

    // Transações do Cartão 1 - Dezembro 2024 (atual)
    {
      id: '10',
      cardId: '1',
      descricao: 'Compras de Natal',
      valor: 450.80,
      data: new Date('2024-12-15'),
      parcela: '1/1',
      createdAt: new Date('2024-12-15')
    },
    {
      id: '11',
      cardId: '1',
      descricao: 'Netflix Assinatura',
      valor: 29.90,
      data: new Date('2024-12-10'),
      parcela: '1/1',
      createdAt: new Date('2024-12-10')
    },

    // Transações do Cartão 2 - Janeiro 2024
    {
      id: '4',
      cardId: '2',
      descricao: 'Notebook Dell',
      valor: 2500.00,
      data: new Date('2024-01-12'),
      parcela: '1/12',
      createdAt: new Date('2024-01-12')
    },

    // Transações do Cartão 2 - Fevereiro 2024
    {
      id: '5',
      cardId: '2',
      descricao: 'Notebook Dell',
      valor: 2500.00,
      data: new Date('2024-02-12'),
      parcela: '2/12',
      createdAt: new Date('2024-01-12')
    },

    // Transações do Cartão 2 - Dezembro 2024
    {
      id: '12',
      cardId: '2',
      descricao: 'Notebook Dell',
      valor: 2500.00,
      data: new Date('2024-12-12'),
      parcela: '12/12',
      createdAt: new Date('2024-01-12')
    },
    {
      id: '13',
      cardId: '2',
      descricao: 'Mouse Logitech',
      valor: 150.00,
      data: new Date('2024-12-05'),
      parcela: '1/1',
      createdAt: new Date('2024-12-05')
    },

    // Transações do Cartão 3 - Janeiro 2024
    {
      id: '6',
      cardId: '3',
      descricao: 'Restaurante Outback',
      valor: 120.00,
      data: new Date('2024-01-13'),
      parcela: '1/2',
      createdAt: new Date('2024-01-13')
    },

    // Transações do Cartão 3 - Fevereiro 2024
    {
      id: '7',
      cardId: '3',
      descricao: 'Restaurante Outback',
      valor: 120.00,
      data: new Date('2024-02-13'),
      parcela: '2/2',
      createdAt: new Date('2024-01-13')
    },

    // Transações do Cartão 3 - Dezembro 2024
    {
      id: '14',
      cardId: '3',
      descricao: 'Ceia de Natal',
      valor: 280.50,
      data: new Date('2024-12-23'),
      parcela: '1/1',
      createdAt: new Date('2024-12-23')
    }
  ];

  constructor(private http: HttpClient) { }

  getCards(): Observable<Card[]> {
    // Retorna dados mockados em vez da API
    return of([...this.mockCards]).pipe(delay(300));
  }

  getCardById(id: string): Observable<Card> {
    const card = this.mockCards.find(c => c.id === id);
    if (!card) {
      throw new Error(`Cartão com ID ${id} não encontrado`);
    }
    return of({ ...card }).pipe(delay(300));
  }

  createCard(card: CreateCardRequest): Observable<Card> {
    const newCard: Card = {
      ...card,
      id: (this.mockCards.length + 1).toString(),
      createdAt: new Date()
    };
    this.mockCards.push(newCard);
    return of(newCard).pipe(
      delay(300),
      tap(() => this.refreshSubject.next())
    );
  }

  updateCard(id: string, card: UpdateCardRequest): Observable<Card> {
    const cardIndex = this.mockCards.findIndex(c => c.id === id);
    if (cardIndex === -1) {
      throw new Error(`Cartão com ID ${id} não encontrado`);
    }

    this.mockCards[cardIndex] = {
      ...this.mockCards[cardIndex],
      ...card
    };

    return of(this.mockCards[cardIndex]).pipe(
      delay(300),
      tap(() => this.refreshSubject.next())
    );
  }

  deleteCard(id: string): Observable<void> {
    const cardIndex = this.mockCards.findIndex(c => c.id === id);
    if (cardIndex === -1) {
      throw new Error(`Cartão com ID ${id} não encontrado`);
    }

    this.mockCards.splice(cardIndex, 1);
    return of(void 0).pipe(
      delay(300),
      tap(() => this.refreshSubject.next())
    );
  }

  // Métodos para transações de cartão
  getTransactionsByCardId(cardId: string): Observable<CardTransaction[]> {
    const transactions = this.mockTransactions.filter(t => t.cardId === cardId);
    return of([...transactions]).pipe(delay(300));
  }

  getTransactionsForInvoice(cardId: string, month: number, year: number): Observable<CardTransaction[]> {
    const transactions = this.mockTransactions.filter(t => {
      if (t.cardId !== cardId) return false;

      const transactionDate = new Date(t.data);
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
