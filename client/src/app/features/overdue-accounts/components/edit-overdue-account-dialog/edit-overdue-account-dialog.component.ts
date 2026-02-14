import { Component, Inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { OverdueAccount } from '../../services/overdue-account.service';

@Component({
  selector: 'app-edit-overdue-account-dialog',
  templateUrl: './edit-overdue-account-dialog.component.html',
  styleUrls: ['./edit-overdue-account-dialog.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule
  ]
})
export class EditOverdueAccountDialogComponent implements OnInit {
  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<EditOverdueAccountDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: OverdueAccount
  ) {
    this.form = this.fb.group({
      description: [data.description, [Validators.required, Validators.maxLength(200)]],
      currentDebtValue: [data.currentDebtValue, [Validators.required, Validators.min(0)]]
    });
  }

  ngOnInit(): void { }

  onSave(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value);
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
