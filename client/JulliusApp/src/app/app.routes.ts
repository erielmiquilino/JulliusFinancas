import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'transactions', pathMatch: 'full' },
  {
    path: 'transactions',
    loadChildren: () => import('./features/financial-transaction/financial-transaction.module')
      .then(m => m.FinancialTransactionModule)
  }
];
