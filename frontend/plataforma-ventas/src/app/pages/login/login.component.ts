import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { environment } from '../../../enviroments/enviromet';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {

  nombreUsuario = '';
  password = '';

  mensaje = '';
  cargando = false;

  private apiUrl = `${environment.apiUrl}/usuarios/login`;

  constructor(private http: HttpClient, private router: Router) {}

  iniciarSesion() {

    this.mensaje = '';
    this.cargando = true;

    const datos = {
      nombreUsuario: this.nombreUsuario,
      password: this.password
    };

    this.http.post<any>(this.apiUrl, datos).subscribe({

      next: (respuesta) => {

        localStorage.setItem('token', respuesta.token);

        localStorage.setItem(
          'usuario',
          JSON.stringify(respuesta.usuario)
        );

        this.mensaje = 'Inicio de sesión correcto';
        this.cargando = false;
        this.router.navigate(['/dashboard']);

        console.log(respuesta);
      },

      error: () => {

        this.mensaje = 'Usuario o contraseña incorrectos';
        this.cargando = false;
        
      }
    });
  }
}