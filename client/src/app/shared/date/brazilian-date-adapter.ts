import { Injectable, Provider } from '@angular/core';
import {
  DateAdapter,
  MAT_DATE_FORMATS,
  MAT_DATE_LOCALE,
  MatDateFormats,
  NativeDateAdapter
} from '@angular/material/core';

/**
 * Formatos do datepicker no padrão brasileiro.
 *
 * O `dateInput` (parse e display) é tratado manualmente pelo BrazilianDateAdapter;
 * os demais continuam sendo opções do Intl.DateTimeFormat.
 */
export const BR_DATE_FORMATS: MatDateFormats = {
  parse: {
    dateInput: 'dd/MM/yyyy'
  },
  display: {
    dateInput: 'dd/MM/yyyy',
    monthYearLabel: { year: 'numeric', month: 'short' },
    dateA11yLabel: { year: 'numeric', month: 'long', day: 'numeric' },
    monthYearA11yLabel: { year: 'numeric', month: 'long' }
  }
};

/** Aceita 15/09/2026, 15-09-2026, 15.09.2026, 15/9/26 e 15092026. */
const DATE_WITH_SEPARATOR = /^(\d{1,2})[\/\-. ](\d{1,2})[\/\-. ](\d{2}|\d{4})$/;
const DATE_ONLY_DIGITS = /^(\d{2})(\d{2})(\d{4})$/;

/**
 * O NativeDateAdapter faz `new Date(Date.parse(value))` e ignora o formato de parse,
 * então digitar "15/09/2026" resulta em data inválida (o Date.parse espera MM/DD/YYYY).
 * Este adapter interpreta a digitação como dd/MM/yyyy e formata a exibição no mesmo padrão.
 */
@Injectable()
export class BrazilianDateAdapter extends NativeDateAdapter {
  override parse(value: unknown, parseFormat?: unknown): Date | null {
    if (typeof value === 'number') {
      return new Date(value);
    }

    if (typeof value !== 'string') {
      return super.parse(value, parseFormat);
    }

    const trimmed = value.trim();
    if (!trimmed) {
      return null;
    }

    const match = DATE_WITH_SEPARATOR.exec(trimmed) ?? DATE_ONLY_DIGITS.exec(trimmed);
    if (!match) {
      // Mantém o comportamento nativo (data inválida) para o datepicker sinalizar o erro.
      return super.parse(value, parseFormat);
    }

    const day = Number(match[1]);
    const month = Number(match[2]);
    const year = this.normalizeYear(Number(match[3]));

    return this.createLocalDate(year, month, day);
  }

  override format(date: Date, displayFormat: unknown): string {
    if (!this.isValid(date)) {
      throw Error('BrazilianDateAdapter: Cannot format invalid date.');
    }

    if (displayFormat === BR_DATE_FORMATS.display.dateInput) {
      return `${this.pad(date.getDate())}/${this.pad(date.getMonth() + 1)}/${date.getFullYear()}`;
    }

    // O NativeDateAdapter formata usando os campos UTC da data; como as datas do app
    // são criadas na meia-noite local, normalizamos para UTC antes de delegar.
    const utcDate = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
    return super.format(utcDate, displayFormat as Object);
  }

  private normalizeYear(year: number): number {
    return year < 100 ? 2000 + year : year;
  }

  /** Cria a data na meia-noite local, rejeitando dias inexistentes (ex.: 31/02). */
  private createLocalDate(year: number, month: number, day: number): Date {
    if (month < 1 || month > 12 || day < 1 || day > 31) {
      return new Date(NaN);
    }

    const date = new Date(year, month - 1, day);
    date.setFullYear(year, month - 1, day);

    const isSameDate =
      date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day;

    return isSameDate ? date : new Date(NaN);
  }

  private pad(value: number): string {
    return String(value).padStart(2, '0');
  }
}

/** Providers do datepicker no padrão brasileiro (adapter + formatos + locale). */
export function provideBrazilianDateAdapter(): Provider[] {
  return [
    { provide: MAT_DATE_LOCALE, useValue: 'pt-BR' },
    { provide: DateAdapter, useClass: BrazilianDateAdapter },
    { provide: MAT_DATE_FORMATS, useValue: BR_DATE_FORMATS }
  ];
}
