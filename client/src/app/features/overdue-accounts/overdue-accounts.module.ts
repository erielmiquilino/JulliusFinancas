import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReactiveFormsModule } from '@angular/forms';

// Material Imports
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialogModule } from '@angular/material/dialog';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule } from '@angular/material/sort';
import { MatTooltipModule } from '@angular/material/tooltip';

// Components
import { OverdueAccountListComponent } from './components/overdue-account-list/overdue-account-list.component';
import { CreateOverdueAccountDialogComponent } from './components/create-overdue-account-dialog/create-overdue-account-dialog.component';
import { EditOverdueAccountDialogComponent } from './components/edit-overdue-account-dialog/edit-overdue-account-dialog.component';
import { OverdueAccountsRoutingModule } from './overdue-accounts-routing.module';

@NgModule({
  declarations: [
  ],
  imports: [
    OverdueAccountListComponent,
    CreateOverdueAccountDialogComponent,
    EditOverdueAccountDialogComponent,
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatDialogModule,
    MatTableModule,
    MatSortModule,
    MatTooltipModule,
    OverdueAccountsRoutingModule
  ]
})
export class OverdueAccountsModule { }
