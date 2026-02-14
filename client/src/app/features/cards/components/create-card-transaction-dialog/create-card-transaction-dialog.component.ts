import { Component, Inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CommonModule, CurrencyPipe, AsyncPipe } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatIconModule } from '@angular/material/icon';
import { Observable, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, startWith } from 'rxjs/operators';
import { CardService, CreateCardTransactionRequest, CardTransactionType } from '../../services/card.service';
import { AutocompleteService } from '../../../../shared/services/autocomplete.service';

@Component({
  selector: 'app-create-card-transaction-dialog',
  templateUrl: './create-card-transaction-dialog.component.html',
  styleUrls: ['./create-card-transaction-dialog.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatDatepickerModule,
    MatSlideToggleModule,
    MatCheckboxModule,
    MatAutocompleteModule,
    MatIconModule,
    AsyncPipe,
    CurrencyPipe
  ]
})
export class CreateCardTransactionDialogComponent implements OnInit {
  form: FormGroup;
  cardId: string;
  invoiceYear: number;
  invoiceMonth: number;
  CardTransactionType = CardTransactionType;
  filteredDescriptions$: Observable<string[]> = of([]);

  constructor(
    private fb: FormBuilder,
    private cardService: CardService,
    private autocompleteService: AutocompleteService,
    private dialogRef: MatDialogRef<CreateCardTransactionDialogComponent>,
    private snackBar: MatSnackBar,
    @Inject(MAT_DIALOG_DATA) public data: { cardId: string, invoiceYear: number, invoiceMonth: number }
  ) {
    this.cardId = data.cardId;
    this.invoiceYear = data.invoiceYear;
    this.invoiceMonth = data.invoiceMonth;

    const today = new Date();
    const localToday = new Date(today.getTime() + today.getTimezoneOffset() * 60000);

    this.form = this.fb.group({
      description: ['', [Validators.required, Validators.maxLength(100)]],
      type: [CardTransactionType.Expense, Validators.required],
      amount: ['', [Validators.required, Validators.min(0.01)]],
      date: [localToday, Validators.required],
      parcelado: [false],
      numeroParcelas: [1, [Validators.min(1), Validators.max(24)]]
    });

    // Observa mudanças no campo parcelado para habilitar/desabilitar número de parcelas
    this.form.get('parcelado')?.valueChanges.subscribe(isParcelado => {
      const numeroParcelasControl = this.form.get('numeroParcelas');
      if (isParcelado) {
        numeroParcelasControl?.setValidators([Validators.required, Validators.min(2), Validators.max(24)]);
        numeroParcelasControl?.setValue(2);
      } else {
        numeroParcelasControl?.setValidators([Validators.min(1), Validators.max(24)]);
        numeroParcelasControl?.setValue(1);
      }
      numeroParcelasControl?.updateValueAndValidity();
    });

    // Observa mudanças no campo tipo para ocultar parcelamento quando for receita
    this.form.get('type')?.valueChanges.subscribe(type => {
      if (type === CardTransactionType.Income) {
        // Se mudou para receita, reseta o parcelamento
        this.form.get('parcelado')?.setValue(false);
        this.form.get('numeroParcelas')?.setValue(1);
      }
    });
  }

  ngOnInit(): void {
    this.setupDescriptionAutocomplete();
  }

  setupDescriptionAutocomplete(): void {
    this.filteredDescriptions$ = this.form.get('description')!.valueChanges.pipe(
      startWith(''),
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(value => this.autocompleteService.getDescriptionSuggestions(value || ''))
    );
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
    const data = new Date(formValue.date);
    const utcData = new Date(data.getTime() - data.getTimezoneOffset() * 60000);

    const transaction: CreateCardTransactionRequest = {
      cardId: this.data.cardId,
      description: formValue.description,
      amount: formValue.amount,
      date: utcData,
      isInstallment: formValue.parcelado,
      installmentCount: formValue.parcelado ? formValue.numeroParcelas : 1,
      type: formValue.type,
      invoiceYear: this.invoiceYear,
      invoiceMonth: this.invoiceMonth
    };

    this.cardService.createCardTransaction(transaction).subscribe({
      next: (response) => {
        console.log('Transação(ões) criada(s) com sucesso:', response);
        const message = this.form.get('parcelado')?.value
          ? `Lançamento parcelado criado com sucesso! ${this.form.get('numeroParcelas')?.value} parcela(s) criada(s).`
          : 'Lançamento criado com sucesso!';
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
      type: CardTransactionType.Expense,
      amount: '',
      date: localToday,
      parcelado: false,
      numeroParcelas: 1
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  get isParcelado(): boolean {
    return this.form.get('parcelado')?.value || false;
  }

  get isReceita(): boolean {
    return this.form.get('type')?.value === CardTransactionType.Income;
  }
}
