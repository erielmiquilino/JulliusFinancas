import { TestBed } from '@angular/core/testing';
import { DateAdapter, MAT_DATE_FORMATS, MatDateFormats } from '@angular/material/core';
import { BR_DATE_FORMATS, provideBrazilianDateAdapter } from './brazilian-date-adapter';

describe('BrazilianDateAdapter', () => {
  let adapter: DateAdapter<Date>;
  let formats: MatDateFormats;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [...provideBrazilianDateAdapter()]
    });

    adapter = TestBed.inject(DateAdapter);
    formats = TestBed.inject(MAT_DATE_FORMATS);
  });

  it('parse_ShouldReturnDate_WhenValueIsTypedAsDdMmYyyy', () => {
    const parsed = adapter.parse('15/09/2026', formats.parse.dateInput) as Date;

    expect(adapter.isValid(parsed)).toBeTrue();
    expect(parsed.getFullYear()).toBe(2026);
    expect(parsed.getMonth()).toBe(8);
    expect(parsed.getDate()).toBe(15);
    expect(parsed.getHours()).toBe(0);
  });

  it('parse_ShouldAcceptAlternateSeparators_WhenValueIsTyped', () => {
    for (const value of ['15-09-2026', '15.09.2026', '15092026', '5/9/26']) {
      const parsed = adapter.parse(value, formats.parse.dateInput) as Date;
      expect(adapter.isValid(parsed)).withContext(value).toBeTrue();
      expect(parsed.getMonth()).withContext(value).toBe(8);
      expect(parsed.getFullYear()).withContext(value).toBe(2026);
    }
  });

  it('parse_ShouldReturnInvalidDate_WhenDayDoesNotExist', () => {
    const parsed = adapter.parse('31/02/2026', formats.parse.dateInput) as Date;

    expect(adapter.isValid(parsed)).toBeFalse();
  });

  it('parse_ShouldReturnNull_WhenValueIsEmpty', () => {
    expect(adapter.parse('', formats.parse.dateInput)).toBeNull();
    expect(adapter.parse('   ', formats.parse.dateInput)).toBeNull();
  });

  it('format_ShouldRenderDdMmYyyy_WhenUsingDateInputFormat', () => {
    const date = new Date(2026, 8, 1);

    expect(adapter.format(date, BR_DATE_FORMATS.display.dateInput)).toBe('01/09/2026');
  });

  it('parse_ShouldRoundTripThroughFormat_WhenValueIsTyped', () => {
    const parsed = adapter.parse('01/09/2026', formats.parse.dateInput) as Date;

    expect(adapter.format(parsed, BR_DATE_FORMATS.display.dateInput)).toBe('01/09/2026');
  });
});
