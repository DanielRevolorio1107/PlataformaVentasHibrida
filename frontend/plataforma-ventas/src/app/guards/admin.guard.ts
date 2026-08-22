import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const adminGuard: CanActivateFn = () => {

  const router = inject(Router);

  const usuarioGuardado = localStorage.getItem('usuario');

  if (!usuarioGuardado) {
    return router.createUrlTree(['/login']);
  }

  try {

    const usuario = JSON.parse(usuarioGuardado);

    if (usuario.rol === 'Administrador') {
      return true;
    }

    return router.createUrlTree(['/dashboard']);

  } catch {

    localStorage.removeItem('token');
    localStorage.removeItem('usuario');

    return router.createUrlTree(['/login']);
  }
};