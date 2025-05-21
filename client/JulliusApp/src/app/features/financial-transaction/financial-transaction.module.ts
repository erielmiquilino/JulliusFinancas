import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReactiveFormsModule } from '@angular/forms';

// Material Imports
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule, MAT_DATE_LOCALE } from '@angular/material/core';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatDialogModule } from '@angular/material/dialog';

// Components
import { CreateTransactionDialogComponent } from './components/create-transaction-dialog/create-transaction-dialog.component';
import { TransactionListComponent } from './components/transaction-list/transaction-list.component';
import { DeleteTransactionDialogComponent } from './components/delete-transaction-dialog/delete-transaction-dialog.component';
import { EditTransactionDialogComponent } from './components/edit-transaction-dialog/edit-transaction-dialog.component';
import { FinancialTransactionRoutingModule } from './financial-transaction-routing.module';

@NgModule({
  declarations: [
    CreateTransactionDialogComponent,
    TransactionListComponent,
    DeleteTransactionDialogComponent,
    EditTransactionDialogComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    FinancialTransactionRoutingModule,
    // Material Modules
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatIconModule,
    MatSnackBarModule,
    MatSelectModule,
    MatDialogModule
  ],
  providers: [
    { provide: MAT_DATE_LOCALE, useValue: 'pt-BR' }
  ]
})
export class FinancialTransactionModule { }
