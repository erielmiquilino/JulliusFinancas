import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { CardService } from '../../services/card.service';

@Component({
  selector: 'app-create-card-dialog',
  templateUrl: './create-card-dialog.component.html',
  styleUrls: ['./create-card-dialog.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule
  ]
})
export class CreateCardDialogComponent {
  form: FormGroup;

  diaFechamentoOptions = Array.from({ length: 31 }, (_, i) => ({
    value: i + 1,
    label: `Dia ${i + 1}`
  }));

  diaVencimentoOptions = Array.from({ length: 31 }, (_, i) => ({
    value: i + 1,
    label: `Dia ${i + 1}`
  }));

  constructor(
    private fb: FormBuilder,
    private cardService: CardService,
    private dialogRef: MatDialogRef<CreateCardDialogComponent>
  ) {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(50)]],
      IssuingBank: ['', [Validators.required, Validators.maxLength(50)]],
      closingDay: ['', [Validators.required, Validators.min(1), Validators.max(31)]],
      dueDay: ['', [Validators.required, Validators.min(1), Validators.max(31)]],
      limit: ['', [Validators.required, Validators.min(1)]]
    });
  }

  onSave(): void {
    if (this.form.valid) {
      const formValue = this.form.value;

      this.cardService.createCard({
        name: formValue.name,
        issuingBank: formValue.IssuingBank,
        closingDay: Number(formValue.closingDay),
        dueDay: Number(formValue.dueDay),
        limit: Number(formValue.limit)
      })
        .subscribe({
          next: () => {
            this.dialogRef.close(true);
          },
          error: (error) => {
            console.error('Erro ao criar cartão:', error);
          }
        });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
