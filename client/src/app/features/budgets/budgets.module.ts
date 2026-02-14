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
import { MatSelectModule } from '@angular/material/select';
import { MatDialogModule } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';

// Components
import { BudgetListComponent } from './components/budget-list/budget-list.component';
import { CreateBudgetDialogComponent } from './components/create-budget-dialog/create-budget-dialog.component';
import { EditBudgetDialogComponent } from './components/edit-budget-dialog/edit-budget-dialog.component';
import { BudgetsRoutingModule } from './budgets-routing.module';

@NgModule({
  declarations: [
  ],
  imports: [
    BudgetListComponent,
    CreateBudgetDialogComponent,
    EditBudgetDialogComponent,
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatSelectModule,
    MatDialogModule,
    MatProgressBarModule,
    MatTooltipModule,
    MatMenuModule,
    BudgetsRoutingModule
  ]
})
export class BudgetsModule { }

