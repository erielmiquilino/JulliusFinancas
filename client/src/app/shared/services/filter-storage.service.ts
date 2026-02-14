import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class FilterStorageService {

  save<T>(key: string, state: T): void {
    try {
      localStorage.setItem(key, JSON.stringify(state));
    } catch (error) {
      console.warn('Erro ao salvar filtros no localStorage:', error);
    }
  }

  load<T>(key: string): T | null {
    try {
      const saved = localStorage.getItem(key);
      if (saved) {
        return JSON.parse(saved) as T;
      }
    } catch (error) {
      console.warn('Erro ao carregar filtros do localStorage:', error);
    }
    return null;
  }

  clear(key: string): void {
    try {
      localStorage.removeItem(key);
    } catch (error) {
      console.warn('Erro ao limpar filtros do localStorage:', error);
    }
  }
}

