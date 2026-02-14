import { Component, Inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { CommonModule, AsyncPipe } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatIconModule } from '@angular/material/icon';
import { Observable, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, startWith } from 'rxjs/operators';
import { CardTransaction, CardTransactionType } from '../../services/card.service';
import { AutocompleteService } from '../../../../shared/services/autocomplete.service';

@Component({
  selector: 'app-edit-card-transaction-dialog',
  templateUrl: './edit-card-transaction-dialog.component.html',
  styleUrls: ['./edit-card-transaction-dialog.component.scss'],
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
    MatAutocompleteModule,
    MatIconModule,
    AsyncPipe
  ]
})
export class EditCardTransactionDialogComponent implements OnInit {
  form: FormGroup;
  CardTransactionType = CardTransactionType;
  filteredDescriptions$: Observable<string[]> = of([]);

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<EditCardTransactionDialogComponent>,
    private autocompleteService: AutocompleteService,
    @Inject(MAT_DIALOG_DATA) public data: CardTransaction & { invoiceYear?: number, invoiceMonth?: number }
  ) {
    // Ajusta a data para o timezone local mantendo o mesmo dia
    const dataTransacao = new Date(data.date);
    const localData = new Date(dataTransacao.getTime() + dataTransacao.getTimezoneOffset() * 60000);

    this.form = this.fb.group({
      description: [data.description, [Validators.required, Validators.maxLength(100)]],
      type: [data.type, Validators.required],
      amount: [data.amount, [Validators.required, Validators.min(0.01)]],
      date: [localData, Validators.required],
      installment: [data.installment, Validators.required]
    });
  }

  ngOnInit(): void {
    this.setupDescriptionAutocomplete();
  }

  setupDescriptionAutocomplete(): void {
    this.filteredDescriptions$ = this.form.get('description')!.valueChanges.pipe(
      startWith(this.form.get('description')?.value || ''),
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(value => this.autocompleteService.getDescriptionSuggestions(value || ''))
    );
  }

  onTypeChange(): void {
    // Pode ser implementado se necessário futuramente
  }

  onSave(): void {
    if (this.form.valid) {
      const formValue = this.form.value;
      const localDate = new Date(formValue.date);
      const utcDate = new Date(localDate.getTime() - localDate.getTimezoneOffset() * 60000);

      const updatedTransaction = {
        description: formValue.description,
        amount: formValue.amount,
        date: utcDate,
        installment: formValue.installment,
        type: formValue.type,
        invoiceYear: this.data.invoiceYear || new Date().getFullYear(),
        invoiceMonth: this.data.invoiceMonth || new Date().getMonth() + 1
      };

      this.dialogRef.close(updatedTransaction);
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  get isReceita(): boolean {
    return this.form.get('type')?.value === CardTransactionType.Income;
  }
}
