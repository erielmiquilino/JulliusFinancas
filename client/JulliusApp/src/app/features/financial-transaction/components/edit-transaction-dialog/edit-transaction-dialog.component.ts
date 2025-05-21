import { Component, Inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { FinancialTransaction, TransactionType } from '../../services/financial-transaction.service';

@Component({
  selector: 'app-edit-transaction-dialog',
  templateUrl: './edit-transaction-dialog.component.html',
  styleUrls: ['./edit-transaction-dialog.component.scss']
})
export class EditTransactionDialogComponent {
  form: FormGroup;
  transactionTypes = Object.values(TransactionType).filter(value => typeof value === 'number');

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<EditTransactionDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: FinancialTransaction
  ) {
    // Ajusta a data para o timezone local mantendo o mesmo dia
    const dueDate = new Date(data.dueDate);
    const localDueDate = new Date(dueDate.getTime() + dueDate.getTimezoneOffset() * 60000);

    this.form = this.fb.group({
      description: [data.description, Validators.required],
      amount: [data.amount, [Validators.required, Validators.min(0.01)]],
      dueDate: [localDueDate, Validators.required],
      type: [data.type, Validators.required]
    });
  }

  onSubmit(): void {
    if (this.form.valid) {
      // Ajusta a data de volta para UTC antes de enviar
      const formValue = this.form.value;
      const dueDate = new Date(formValue.dueDate);
      const utcDueDate = new Date(dueDate.getTime() - dueDate.getTimezoneOffset() * 60000);

      this.dialogRef.close({
        ...formValue,
        dueDate: utcDueDate
      });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
