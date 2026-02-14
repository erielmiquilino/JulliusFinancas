import { Component, Inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { CommonModule, CurrencyPipe, AsyncPipe } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Observable, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, startWith } from 'rxjs/operators';
import { FinancialTransaction, TransactionType } from '../../services/financial-transaction.service';
import { CategoryService, Category } from '../../../categories/services/category.service';
import { BudgetService, Budget } from '../../../budgets/services/budget.service';
import { AutocompleteService } from '../../../../shared/services/autocomplete.service';

@Component({
  selector: 'app-edit-transaction-dialog',
  templateUrl: './edit-transaction-dialog.component.html',
  styleUrls: ['./edit-transaction-dialog.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatSlideToggleModule,
    MatCheckboxModule,
    MatAutocompleteModule,
    MatButtonModule,
    MatIconModule,
    AsyncPipe
  ]
})
export class EditTransactionDialogComponent implements OnInit {
  form: FormGroup;
  categories: Category[] = [];
  budgets: Budget[] = [];
  transactionTypes = Object.values(TransactionType).filter(value => typeof value === 'number');
  filteredDescriptions$: Observable<string[]> = of([]);

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<EditTransactionDialogComponent>,
    private categoryService: CategoryService,
    private budgetService: BudgetService,
    private autocompleteService: AutocompleteService,
    @Inject(MAT_DIALOG_DATA) public data: FinancialTransaction
  ) {
    // Ajusta a data para o timezone local mantendo o mesmo dia
    const dueDate = new Date(data.dueDate);
    const localDueDate = new Date(dueDate.getTime() + dueDate.getTimezoneOffset() * 60000);

    this.form = this.fb.group({
      description: [data.description, Validators.required],
      amount: [data.amount, [Validators.required, Validators.min(0.01)]],
      dueDate: [localDueDate, Validators.required],
      type: [data.type, Validators.required],
      categoryId: [data.categoryId, Validators.required],
      budgetId: [data.budgetId || null],
      isPaid: [data.isPaid || false]
    });

    // Observa mudanças na data de vencimento para carregar budgets do período
    this.form.get('dueDate')?.valueChanges.subscribe(newDueDate => {
      if (newDueDate) {
        this.loadBudgetsForDate(newDueDate);
      }
    });
  }

  ngOnInit(): void {
    this.loadCategories();
    this.setupDescriptionAutocomplete();
    // Carrega budgets para a data atual da transação
    const dueDate = new Date(this.data.dueDate);
    this.loadBudgetsForDate(dueDate);
  }

  setupDescriptionAutocomplete(): void {
    this.filteredDescriptions$ = this.form.get('description')!.valueChanges.pipe(
      startWith(this.form.get('description')?.value || ''),
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(value => this.autocompleteService.getDescriptionSuggestions(value || ''))
    );
  }

  loadCategories(): void {
    this.categoryService.getAllCategories().subscribe({
      next: (categories) => {
        this.categories = categories;
      },
      error: (error) => {
        console.error('Erro ao carregar categorias:', error);
      }
    });
  }

  loadBudgetsForDate(date: Date): void {
    const month = date.getMonth() + 1;
    const year = date.getFullYear();

    this.budgetService.getBudgetsByPeriod(month, year).subscribe({
      next: (budgets) => {
        this.budgets = budgets;
        // Se o budget selecionado não está mais disponível, limpa a seleção
        const currentBudgetId = this.form.get('budgetId')?.value;
        if (currentBudgetId && !budgets.find(b => b.id === currentBudgetId)) {
          this.form.get('budgetId')?.setValue(null);
        }
      },
      error: (error) => {
        console.error('Erro ao carregar budgets:', error);
        this.budgets = [];
      }
    });
  }

  onSave(): void {
    if (this.form.valid) {
      // Ajusta a data de volta para UTC antes de enviar
      const formValue = this.form.value;
      const dueDate = new Date(formValue.dueDate);
      const utcDueDate = new Date(dueDate.getTime() - dueDate.getTimezoneOffset() * 60000);

      this.dialogRef.close({
        ...this.data,
        ...formValue,
        dueDate: utcDueDate,
        budgetId: formValue.budgetId || undefined
      });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
