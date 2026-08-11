import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './usuarios.component.html',
  styleUrl: './usuarios.component.scss'
})
export class UsuariosComponent implements OnInit {

  usuarios: any[] = [];
  roles: any[] = [];

  mostrarFormulario = false;
  cargando = false;
  mensaje = '';

  nuevoUsuario = {
    nombreCompleto: '',
    nombreUsuario: '',
    password: '',
    rolId: ''
  };

  private usuariosUrl = 'http://localhost:5080/api/usuarios';
  private rolesUrl = 'http://localhost:5080/api/estado/base-datos';

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.cargarUsuarios();
    this.cargarRoles();
  }

  obtenerHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');

    return new HttpHeaders({
      Authorization: `Bearer ${token}`
    });
  }

  cargarUsuarios(): void {
    this.http.get<any[]>(
      this.usuariosUrl,
      { headers: this.obtenerHeaders() }
    ).subscribe({
      next: (usuarios) => {
        this.usuarios = usuarios;
      },

      error: () => {
        this.mensaje = 'No se pudieron cargar los usuarios.';
      }
    });
  }

  cargarRoles(): void {
    this.http.get<any>(
      this.rolesUrl,
      { headers: this.obtenerHeaders() }
    ).subscribe({
      next: (respuesta) => {
        this.roles = respuesta.roles;
      },

      error: () => {
        this.mensaje = 'No se pudieron cargar los roles.';
      }
    });
  }

  crearUsuario(): void {

    if (!this.nuevoUsuario.nombreCompleto.trim()) {
      this.mensaje = 'El nombre completo es obligatorio.';
      return;
    }

    if (!this.nuevoUsuario.nombreUsuario.trim()) {
      this.mensaje = 'El nombre de usuario es obligatorio.';
      return;
    }

    if (this.nuevoUsuario.password.length < 8) {
      this.mensaje = 'La contraseña debe tener al menos 8 caracteres.';
      return;
    }

    if (!this.nuevoUsuario.rolId) {
      this.mensaje = 'Selecciona un rol.';
      return;
    }

    this.cargando = true;
    this.mensaje = '';

    this.http.post(
      this.usuariosUrl,
      this.nuevoUsuario,
      { headers: this.obtenerHeaders() }
    ).subscribe({
      next: () => {
        this.mensaje = 'Usuario creado correctamente.';
        this.cargando = false;
        this.mostrarFormulario = false;

        this.nuevoUsuario = {
          nombreCompleto: '',
          nombreUsuario: '',
          password: '',
          rolId: ''
        };

        this.cargarUsuarios();
      },

      error: (error) => {
        this.cargando = false;

        if (error.status === 409) {
          this.mensaje = 'El nombre de usuario ya existe.';
          return;
        }

        this.mensaje = 'No se pudo crear el usuario.';
      }
    });
  }

  desactivarUsuario(usuario: any): void {

    if (!confirm(`¿Desactivar al usuario "${usuario.nombreUsuario}"?`)) {
      return;
    }

    this.http.patch(
      `${this.usuariosUrl}/${usuario.id}/desactivar`,
      {},
      { headers: this.obtenerHeaders() }
    ).subscribe({
      next: () => {
        this.mensaje = 'Usuario desactivado correctamente.';
        this.cargarUsuarios();
      },

      error: (error) => {
        this.mensaje =
          error.error?.mensaje || 'No se pudo desactivar el usuario.';
      }
    });
  }

  activarUsuario(usuario: any): void {

    this.http.patch(
      `${this.usuariosUrl}/${usuario.id}/activar`,
      {},
      { headers: this.obtenerHeaders() }
    ).subscribe({
      next: () => {
        this.mensaje = 'Usuario activado correctamente.';
        this.cargarUsuarios();
      },

      error: () => {
        this.mensaje = 'No se pudo activar el usuario.';
      }
    });
  }
}