import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';

export enum TransactionType {
  PayableBill = 0,
  ReceivableBill = 1
}

export enum ReconciliationSessionStatus {
  Draft = 0,
  Confirmed = 1,
  Discarded = 2
}

export enum ReconciliationItemStatus {
  Pending = 0,
  Approved = 1,
  Ignored = 2,
  NettedInternal = 3,
  Posted = 4
}

export enum ReconciliationReviewFlag {
  None = 0,
  AmbiguousCategory = 1,
  OrphanTransfer = 2,
  PossibleDuplicate = 3
}

export interface BankAccount {
  id: string;
  name: string;
  institution: string;
  holderName: string;
  pluggyItemId: string;
  pluggyAccountId: string;
  openingBalance: number;
  openingBalanceDate: string | null;
  hasOpeningBalance: boolean;
  lastKnownBalance: number;
  lastBalanceSyncedAt: string | null;
  lastSyncedAt: string | null;
  isActive: boolean;
  createdAt: string;
  isConnectionAlive?: boolean | null;
  connectionMessage?: string | null;
}

export interface CreateBankAccountRequest {
  name: string;
  institution: string;
  holderName: string;
  pluggyItemId: string;
  pluggyAccountId: string;
}

export interface DiscoveredAccount {
  pluggyAccountId: string;
  name: string;
  number: string | null;
  subtype: string | null;
  owner: string | null;
  balance: number;
  isCreditCard: boolean;
  alreadyRegistered: boolean;
}

export interface ConsolidatedAccountBalance {
  bankAccountId: string;
  name: string;
  institution: string;
  lastKnownBalance: number;
  lastBalanceSyncedAt: string | null;
  isNegative: boolean;
}

export interface ConsolidatedBalance {
  isConfigured: boolean;
  isHistoricalPeriod: boolean;
  openingBalanceDate: string | null;
  emConta: number;
  saldoBancos: number;
  divergencia: number | null;
  saldoBancosAtualizadoEm: string | null;
  contas: ConsolidatedAccountBalance[];
}

export interface ReconciliationItem {
  id: string;
  bankAccountId: string;
  bankAccountName: string;
  rawDescription: string;
  rawAmount: number;
  absoluteAmount: number;
  rawDate: string;
  rawCategory: string | null;
  counterpartyName: string | null;
  paymentMethod: string | null;
  proposedDescription: string;
  proposedCategoryId: string | null;
  proposedCategoryName: string | null;
  proposedType: TransactionType;
  status: ReconciliationItemStatus;
  reviewFlag: ReconciliationReviewFlag;
  matchedItemId: string | null;
  reviewReason: string | null;
}

export interface ReconciliationSession {
  id: string;
  periodFrom: string;
  periodTo: string;
  status: ReconciliationSessionStatus;
  startedAt: string;
  closedAt: string | null;
  totalItems: number;
  needsAttentionCount: number;
  readyCount: number;
  nettedCount: number;
  totalIncome: number;
  totalExpenses: number;
  netAmount: number;
  projectedBalance: number;
  bankBalance: number;
  items: ReconciliationItem[];
  warnings: string[];
}

export interface SyncResult {
  sessionId: string | null;
  importedCount: number;
  skippedCount: number;
  nettedCount: number;
  warnings: string[];
  message: string;
}

export interface UpdateReconciliationItemRequest {
  description: string;
  categoryId: string | null;
  status: ReconciliationItemStatus;
}

export interface ConfirmResult {
  postedCount: number;
  ignoredCount: number;
  nettedCount: number;
  emConta: number;
  saldoBancos: number;
  divergencia: number;
}

@Injectable({
  providedIn: 'root'
})
export class ReconciliationService {
  private readonly http = inject(HttpClient);
  private readonly accountsUrl = `${environment.apiUrl}/BankAccount`;
  private readonly reconciliationUrl = `${environment.apiUrl}/Reconciliation`;
  private readonly refreshList = new Subject<void>();

  readonly refresh$ = this.refreshList.asObservable();

  // --- Contas bancárias ---

  getAccounts(): Observable<BankAccount[]> {
    // O endpoint usa [EnableQuery], então pode devolver array cru ou { value: [...] }.
    return this.http
      .get<any>(this.accountsUrl)
      .pipe(map(res => (Array.isArray(res) ? res : (res?.value ?? []))));
  }

  checkConnections(): Observable<BankAccount[]> {
    return this.http.get<BankAccount[]>(`${this.accountsUrl}/connections`);
  }

  discoverAccounts(pluggyItemId: string): Observable<DiscoveredAccount[]> {
    return this.http.get<DiscoveredAccount[]>(`${this.accountsUrl}/discover/${pluggyItemId}`);
  }

  createAccount(request: CreateBankAccountRequest): Observable<BankAccount> {
    return this.http
      .post<BankAccount>(this.accountsUrl, request)
      .pipe(tap(() => this.refreshList.next()));
  }

  deleteAccount(id: string): Observable<void> {
    return this.http
      .delete<void>(`${this.accountsUrl}/${id}`)
      .pipe(tap(() => this.refreshList.next()));
  }

  setOpeningBalance(id: string, openingBalanceDate: Date): Observable<BankAccount> {
    return this.http
      .post<BankAccount>(`${this.accountsUrl}/${id}/opening-balance`, { openingBalanceDate })
      .pipe(tap(() => this.refreshList.next()));
  }

  clearOpeningBalance(id: string): Observable<BankAccount> {
    return this.http
      .delete<BankAccount>(`${this.accountsUrl}/${id}/opening-balance`)
      .pipe(tap(() => this.refreshList.next()));
  }

  getConsolidatedBalance(month: number, year: number): Observable<ConsolidatedBalance> {
    return this.http.get<ConsolidatedBalance>(
      `${this.accountsUrl}/consolidated-balance?month=${month}&year=${year}`
    );
  }

  // --- Conciliação ---

  sync(from?: Date): Observable<SyncResult> {
    return this.http
      .post<SyncResult>(`${this.reconciliationUrl}/sync`, { from: from ?? null })
      .pipe(tap(() => this.refreshList.next()));
  }

  getOpenSession(): Observable<ReconciliationSession | null> {
    return this.http.get<ReconciliationSession | null>(`${this.reconciliationUrl}/sessions/open`);
  }

  getSession(id: string): Observable<ReconciliationSession> {
    return this.http.get<ReconciliationSession>(`${this.reconciliationUrl}/sessions/${id}`);
  }

  updateItem(id: string, request: UpdateReconciliationItemRequest): Observable<ReconciliationItem> {
    return this.http.put<ReconciliationItem>(`${this.reconciliationUrl}/items/${id}`, request);
  }

  confirmSession(id: string): Observable<ConfirmResult> {
    return this.http
      .post<ConfirmResult>(`${this.reconciliationUrl}/sessions/${id}/confirm`, {})
      .pipe(tap(() => this.refreshList.next()));
  }

  discardSession(id: string): Observable<void> {
    return this.http
      .post<void>(`${this.reconciliationUrl}/sessions/${id}/discard`, {})
      .pipe(tap(() => this.refreshList.next()));
  }

  getIgnoredItems(): Observable<ReconciliationItem[]> {
    return this.http.get<ReconciliationItem[]>(`${this.reconciliationUrl}/ignored`);
  }
}
