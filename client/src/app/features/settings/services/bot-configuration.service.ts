import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';

export interface BotConfigurationDto {
  configKey: string;
  description: string;
  hasValue: boolean;
  updatedAt: string | null;
}

export interface UpdateBotConfigurationRequest {
  value: string;
  description: string;
}

export interface TestResult {
  success: boolean;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class BotConfigurationService {
  private apiUrl = `${environment.apiUrl}/BotConfiguration`;
  private refreshList = new Subject<void>();

  constructor(private http: HttpClient) { }

  get refresh$() {
    return this.refreshList.asObservable();
  }

  getAll(): Observable<BotConfigurationDto[]> {
    return this.http.get<BotConfigurationDto[]>(this.apiUrl);
  }

  getByKey(key: string): Observable<{ value: string }> {
    return this.http.get<{ value: string }>(`${this.apiUrl}/${key}`);
  }

  upsert(key: string, request: UpdateBotConfigurationRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${key}`, request)
      .pipe(tap(() => this.refreshList.next()));
  }

  delete(key: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${key}`)
      .pipe(tap(() => this.refreshList.next()));
  }

  testTelegram(): Observable<TestResult> {
    return this.http.post<TestResult>(`${this.apiUrl}/test-telegram`, {});
  }

  testGemini(): Observable<TestResult> {
    return this.http.post<TestResult>(`${this.apiUrl}/test-gemini`, {});
  }

  registerWebhook(baseUrl: string): Observable<TestResult> {
    return this.http.post<TestResult>(`${this.apiUrl}/register-webhook`, { baseUrl });
  }
}
