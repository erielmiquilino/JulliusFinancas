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
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatMenuModule } from '@angular/material/menu';

// Components
import { CardListComponent } from './components/card-list/card-list.component';
import { CardTransactionListComponent } from './components/card-transaction-list/card-transaction-list.component';
import { CreateCardDialogComponent } from './components/create-card-dialog/create-card-dialog.component';
import { DeleteCardDialogComponent } from './components/delete-card-dialog/delete-card-dialog.component';
import { EditCardDialogComponent } from './components/edit-card-dialog/edit-card-dialog.component';
import { CreateCardTransactionDialogComponent } from './components/create-card-transaction-dialog/create-card-transaction-dialog.component';
import { DeleteCardTransactionDialogComponent } from './components/delete-card-transaction-dialog/delete-card-transaction-dialog.component';
import { EditCardTransactionDialogComponent } from './components/edit-card-transaction-dialog/edit-card-transaction-dialog.component';
import { CardRoutingModule } from './card-routing.module';

@NgModule({
  declarations: [
    CardListComponent,
    CardTransactionListComponent,
    CreateCardDialogComponent,
    DeleteCardDialogComponent,
    EditCardDialogComponent,
    CreateCardTransactionDialogComponent,
    DeleteCardTransactionDialogComponent,
    EditCardTransactionDialogComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
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
    MatDialogModule,
    MatSlideToggleModule,
    MatProgressSpinnerModule,
    MatMenuModule,
    CardRoutingModule
  ],
  providers: [
    { provide: MAT_DATE_LOCALE, useValue: 'pt-BR' }
  ]
})
export class CardModule { }
