import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
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
import { FinancialTransactionService, TransactionType } from '../../services/financial-transaction.service';
import { CategoryService, Category } from '../../../categories/services/category.service';
import { BudgetService, Budget } from '../../../budgets/services/budget.service';
import { AutocompleteService } from '../../../../shared/services/autocomplete.service';

@Component({
  selector: 'app-create-transaction-dialog',
  templateUrl: './create-transaction-dialog.component.html',
  styleUrls: ['./create-transaction-dialog.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatSnackBarModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatSlideToggleModule,
    MatCheckboxModule,
    MatAutocompleteModule,
    MatButtonModule,
    MatIconModule,
    CurrencyPipe,
    AsyncPipe
  ]
})
export class CreateTransactionDialogComponent implements OnInit {
  form: FormGroup;
  TransactionType = TransactionType;
  categories: Category[] = [];
  budgets: Budget[] = [];
  transactionTypes = [
    { value: TransactionType.PayableBill, label: 'Conta a Pagar' },
    { value: TransactionType.ReceivableBill, label: 'Conta a Receber' }
  ];
  filteredDescriptions$: Observable<string[]> = of([]);

  constructor(
    private fb: FormBuilder,
    private transactionService: FinancialTransactionService,
    private categoryService: CategoryService,
    private budgetService: BudgetService,
    private autocompleteService: AutocompleteService,
    private dialogRef: MatDialogRef<CreateTransactionDialogComponent>,
    private snackBar: MatSnackBar
  ) {
    const today = new Date();
    const localToday = new Date(today.getTime() + today.getTimezoneOffset() * 60000);

    this.form = this.fb.group({
      description: ['', Validators.required],
      amount: ['', [Validators.required, Validators.min(0.01)]],
      dueDate: [localToday, Validators.required],
      type: [TransactionType.PayableBill, Validators.required],
      categoryId: ['', Validators.required],
      budgetId: [null],
      isPaid: [false],
      isInstallment: [false],
      installmentCount: [1, [Validators.min(1), Validators.max(24)]]
    });

    // Observa mudanças no campo isInstallment para habilitar/desabilitar número de parcelas
    this.form.get('isInstallment')?.valueChanges.subscribe(isInstallment => {
      const installmentCountControl = this.form.get('installmentCount');
      if (isInstallment) {
        installmentCountControl?.setValidators([Validators.required, Validators.min(2), Validators.max(24)]);
        installmentCountControl?.setValue(2);
      } else {
        installmentCountControl?.setValidators([Validators.min(1), Validators.max(24)]);
        installmentCountControl?.setValue(1);
      }
      installmentCountControl?.updateValueAndValidity();
    });

    // Observa mudanças no campo tipo para ocultar parcelamento quando for conta a receber
    this.form.get('type')?.valueChanges.subscribe(type => {
      if (type === TransactionType.ReceivableBill) {
        // Se mudou para conta a receber, reseta o parcelamento
        this.form.get('isInstallment')?.setValue(false);
        this.form.get('installmentCount')?.setValue(1);
      }
    });

    // Observa mudanças na data de vencimento para carregar budgets do período
    this.form.get('dueDate')?.valueChanges.subscribe(dueDate => {
      if (dueDate) {
        this.loadBudgetsForDate(dueDate);
      }
    });
  }

  ngOnInit(): void {
    this.loadCategories();
    this.setupDescriptionAutocomplete();
    // Carrega budgets para a data inicial
    const initialDate = this.form.get('dueDate')?.value;
    if (initialDate) {
      this.loadBudgetsForDate(initialDate);
    }
  }

  setupDescriptionAutocomplete(): void {
    this.filteredDescriptions$ = this.form.get('description')!.valueChanges.pipe(
      startWith(''),
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

  onTypeChange(): void {
    // Lógica já tratada no subscribe do valueChanges no constructor
  }

  onSave(): void {
    if (this.form.valid) {
      this.saveTransaction(true);
    }
  }

  onSaveAndNew(): void {
    if (this.form.valid) {
      this.saveTransaction(false);
    }
  }

  private saveTransaction(shouldClose: boolean): void {
    const formValue = this.form.value;
    const dueDate = new Date(formValue.dueDate);
    const utcDueDate = new Date(dueDate.getTime() - dueDate.getTimezoneOffset() * 60000);

    this.transactionService.createTransaction({
      description: formValue.description,
      amount: formValue.amount,
      dueDate: utcDueDate,
      type: formValue.type,
      categoryId: formValue.categoryId,
      budgetId: formValue.budgetId || undefined,
      isPaid: formValue.isPaid,
      isInstallment: formValue.isInstallment,
      installmentCount: formValue.isInstallment ? formValue.installmentCount : 1
    })
      .subscribe({
        next: (response) => {
          console.log('Transação(ões) criada(s) com sucesso:', response);
          const message = this.form.get('isInstallment')?.value
            ? `Transação parcelada criada com sucesso! ${this.form.get('installmentCount')?.value} parcela(s) criada(s).`
            : 'Transação criada com sucesso!';
          this.snackBar.open(message, 'Fechar', {
            duration: 3000,
            panelClass: ['success-snackbar']
          });
          if (shouldClose) {
            this.dialogRef.close(true);
          } else {
            this.resetForm();
          }
        },
        error: (error) => {
          console.error('Erro ao criar transação:', error);
        }
      });
  }

  private resetForm(): void {
    const today = new Date();
    const localToday = new Date(today.getTime() + today.getTimezoneOffset() * 60000);

    this.form.reset({
      description: '',
      amount: '',
      dueDate: localToday,
      type: TransactionType.PayableBill,
      categoryId: '',
      budgetId: null,
      isPaid: false,
      isInstallment: false,
      installmentCount: 1
    });

    // Recarrega budgets para a nova data
    this.loadBudgetsForDate(localToday);
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  get isInstallment(): boolean {
    return this.form.get('isInstallment')?.value || false;
  }

  get isReceivableBill(): boolean {
    return this.form.get('type')?.value === TransactionType.ReceivableBill;
  }
}
