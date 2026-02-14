import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CardListComponent } from './components/card-list/card-list.component';
import { CardTransactionListComponent } from './components/card-transaction-list/card-transaction-list.component';

const routes: Routes = [
  { path: '', component: CardListComponent },
  { path: ':id/transactions', component: CardTransactionListComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class CardRoutingModule { }
