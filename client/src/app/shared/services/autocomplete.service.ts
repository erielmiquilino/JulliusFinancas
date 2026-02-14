import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AutocompleteService {
  private readonly apiUrl = `${environment.apiUrl}/Autocomplete`;

  constructor(private http: HttpClient) { }

  getDescriptionSuggestions(searchTerm: string): Observable<string[]> {
    if (!searchTerm || searchTerm.length < 2) {
      return of([]);
    }

    const params = new HttpParams().set('search', searchTerm);
    
    return this.http.get<string[]>(`${this.apiUrl}/descriptions`, { params }).pipe(
      catchError(() => of([]))
    );
  }
}

