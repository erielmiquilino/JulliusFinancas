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
    const dataTransacao = new Date(data.data);
    const localData = new Date(dataTransacao.getTime() + dataTransacao.getTimezoneOffset() * 60000);

    this.form = this.fb.group({
      descricao: [data.descricao, [Validators.required, Validators.maxLength(100)]],
      valor: [data.valor, [Validators.required, Validators.min(0.01)]],
      data: [localData, Validators.required],
      parcela: [data.parcela, Validators.required]
    });
  }

  onSubmit(): void {
    if (this.form.valid) {
      // Ajusta a data de volta para UTC antes de enviar
      const formValue = this.form.value;
      const data = new Date(formValue.data);
      const utcData = new Date(data.getTime() - data.getTimezoneOffset() * 60000);

      this.dialogRef.close({
        ...this.data,
        ...formValue,
        data: utcData
      });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
