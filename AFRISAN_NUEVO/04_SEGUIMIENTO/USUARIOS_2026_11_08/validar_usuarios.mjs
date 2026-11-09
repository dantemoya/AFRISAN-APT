// Gestión de cuentas (08/11/2026): activar/desactivar, cambiar rol y restablecer contraseña.
// Se ejecuta desde ../PERSISTENCIA_2026_11_02/validar_api.mjs con AFRISAN_TEST_USUARIOS=1,
// después del recorrido completo, sobre PostgreSQL embebido aislado (sin Supabase).
import {randomUUID} from 'node:crypto';

export async function validarUsuarios({api,check,login,password,admin,bodega,autorizador,request}){
 const buscar=async correo=>(await api('usuarios',admin)).find(u=>u.correo===correo);

 // El Administrador no puede bloquearse a sí mismo.
 const propio=await buscar('admin@example.invalid');
 await api(`usuarios/${propio.id}/estado`,admin,{activo:false},'PUT',409);
 await api(`usuarios/${propio.id}/rol`,admin,{rol:'Bodega'},'PUT',409);
 check((await buscar('admin@example.invalid')).activo,'El Administrador no puede desactivarse ni cambiar su propio rol');

 // Solo el Administrador gestiona cuentas.
 const temporal=await api('usuarios',admin,{nombre:'Cuenta',apellido:'Temporal',correo:'temporal@example.invalid',rol:'Bodega',password});
 await api(`usuarios/${temporal.id}/estado`,bodega,{activo:false},'PUT',403);
 await api(`usuarios/${temporal.id}/clave`,bodega,{password:'Otra-Clave-Larga-2026'},'PUT',403);

 // Desactivar cierra la sesión abierta e impide entrar; reactivar lo permite de nuevo.
 let sesion=await login('temporal@example.invalid');
 await api('refrigerantes',sesion);
 check((await api(`usuarios/${temporal.id}/estado`,admin,{activo:false},'PUT')).activo===false,'Desactivar devuelve la cuenta inactiva');
 await api('refrigerantes',sesion,undefined,'GET',401);
 await api('sesiones',null,{email:'temporal@example.invalid',password},'POST',401);
 check((await api(`usuarios/${temporal.id}/estado`,admin,{activo:false},'PUT')).activo===false,'Repetir la desactivación no falla');
 await api(`usuarios/${temporal.id}/estado`,admin,{activo:true},'PUT');
 sesion=await login('temporal@example.invalid');

 // Cambiar el rol cierra la sesión anterior; la nueva sesión usa el rol actualizado.
 check((await api(`usuarios/${temporal.id}/rol`,admin,{rol:'Jefatura'},'PUT')).rol==='Jefatura','El cambio de rol se guarda');
 await api('refrigerantes',sesion,undefined,'GET',401);
 check((await api('sesiones',null,{email:'temporal@example.invalid',password})).rol==='Jefatura','La sesión nueva usa el rol actualizado');

 // Datos inválidos.
 await api(`usuarios/${temporal.id}/rol`,admin,{rol:'SuperAdmin'},'PUT',400);
 await api(`usuarios/${temporal.id}/clave`,admin,{password:'corta'},'PUT',400);
 await api('usuarios/999999/estado',admin,{activo:false},'PUT',404);

 // "otro" quedó bloqueado por cinco intentos fallidos en el recorrido principal.
 const otro=await buscar('otro@example.invalid');
 check(otro.bloqueado===true,'El listado muestra la cuenta bloqueada temporalmente');
 const nueva='Clave-Restablecida-'+randomUUID();
 check((await api(`usuarios/${otro.id}/clave`,admin,{password:nueva},'PUT')).bloqueado===false,'Restablecer la contraseña quita el bloqueo');
 await api('sesiones',null,{email:'otro@example.invalid',password},'POST',401);
 check(!!(await api('sesiones',null,{email:'otro@example.invalid',password:nueva})).token,'La contraseña nueva permite entrar y la anterior no');

 // Un técnico con OT abierta no puede quedar inactivo ni cambiar de rol.
 const tecnico=await buscar('tecnico@example.invalid');
 await api('ordenes-trabajo',autorizador,{...request,idOperacion:randomUUID(),descripcion:'OT abierta para prueba de cuentas',cilindrosARetirar:[]});
 await api(`usuarios/${tecnico.id}/estado`,admin,{activo:false},'PUT',409);
 await api(`usuarios/${tecnico.id}/rol`,admin,{rol:'Bodega'},'PUT',409);
 check((await buscar('tecnico@example.invalid')).activo,'Un técnico con OT abierta no se desactiva ni cambia de rol');
}
