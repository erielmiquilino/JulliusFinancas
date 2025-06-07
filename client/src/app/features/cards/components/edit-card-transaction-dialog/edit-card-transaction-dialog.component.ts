import { Component, Inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { CardTransaction } from '../../services/card.service';

@Component({
  selector: 'app-edit-card-transaction-dialog',
  templateUrl: './edit-card-transaction-dialog.component.html',
  styleUrls: ['./edit-card-transaction-dialog.component.scss']
})
export class EditCardTransactionDialogComponent {
  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<EditCardTransactionDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CardTransaction
  ) {
    // Ajusta a data para o timezone local mantendo o mesmo dia
    const dataTransacao = new Date(data.date);
    const localData = new Date(dataTransacao.getTime() + dataTransacao.getTimezoneOffset() * 60000);

    this.form = this.fb.group({
      description: [data.description, [Validators.required, Validators.maxLength(100)]],
      amount: [data.amount, [Validators.required, Validators.min(0.01)]],
      date: [localData, Validators.required],
      installment: [data.installment, Validators.required]
    });
  }

  onSubmit(): void {
    if (this.form.valid) {
      const formValue = this.form.value;
      const localDate = new Date(formValue.date);
      const utcDate = new Date(localDate.getTime() - localDate.getTimezoneOffset() * 60000);

      const updatedTransaction = {
        description: formValue.description,
        amount: formValue.amount,
        date: utcDate,
        installment: formValue.installment
      };

      this.dialogRef.close(updatedTransaction);
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
