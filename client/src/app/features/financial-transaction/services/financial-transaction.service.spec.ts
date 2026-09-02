import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  FinancialTransactionService,
  TransactionType
} from './financial-transaction.service';

describe('FinancialTransactionService (filtro de datas)', () => {
  let service: FinancialTransactionService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        FinancialTransactionService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(FinancialTransactionService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function captureFilter(request: () => void): string {
    request();
    const req = httpMock.expectOne(r => r.url.includes('FinancialTransaction'));
    req.flush([]);
    return decodeURIComponent(req.request.urlWithParams);
  }

  it('getAllTransactions_ShouldStartRangeAtUtcMidnight_WhenDateRangeIsCustom', () => {
    const url = captureFilter(() =>
      service.getAllTransactions({
        dateRangeType: 'Custom',
        startDate: new Date(2026, 8, 1), // 01/09/2026 meia-noite local
        endDate: new Date(2026, 8, 10)
      }).subscribe()
    );

    expect(url).toContain('DueDate ge 2026-09-01T00:00:00.000Z');
    expect(url).toContain('DueDate le 2026-09-10T23:59:59.999Z');
  });

  it('getAllTransactions_ShouldKeepMonthBoundaries_WhenDateRangeIsMonth', () => {
    const url = captureFilter(() =>
      service.getAllTransactions({
        dateRangeType: 'Month',
        month: 9,
        year: 2026,
        type: TransactionType.PayableBill
      }).subscribe()
    );

    expect(url).toContain('DueDate ge 2026-09-01T00:00:00.000Z');
    expect(url).toContain('DueDate le 2026-09-30T23:59:59.999Z');
    expect(url).toContain("Type eq 'PayableBill'");
  });

  it('getAllTransactions_ShouldKeepMonthBoundaries_WhenDateRangeTypeIsMissing', () => {
    const url = captureFilter(() =>
      service.getAllTransactions({ month: 2, year: 2024 }).subscribe()
    );

    expect(url).toContain('DueDate ge 2024-02-01T00:00:00.000Z');
    expect(url).toContain('DueDate le 2024-02-29T23:59:59.999Z');
  });
});
