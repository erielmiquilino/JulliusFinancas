import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { OverdueAccountListComponent } from './components/overdue-account-list/overdue-account-list.component';

const routes: Routes = [
  { path: '', component: OverdueAccountListComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class OverdueAccountsRoutingModule { }
