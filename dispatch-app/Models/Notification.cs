namespace dispatch_app.Models
{
    public class Notification
    {
        public const string Permission = "No tienes permisos para: {0}";
        public const string List = "Listar ";
        public const string Add = "Agregar ";
        public const string Update = "Actualizar ";
        public const string Delete = "Eliminar ";
        public const string Reset = "Reiniciar ";
        public const string AuthActive = "La autenticación de 2 factores está activa";
        public const string AuthDisable = "La autenticación de 2 factores NO está activa";
        public const string AuthEmpty = "El código de autenticación no puede estar vacío";
        public const string AuthIncorrect = "El código de autenticación no es correcto";
        public const string UserPasswordError = "Usuario y/o Contraseña incorrecta";
        public const string UserError = "Correo o Usuario incorrecto";
        public const string PasswordError = "Contraseña incorrecta";
        public const string UserValidated = "El usuario aun no ha sido validado";
        public const string UserNotFound = "El usuario no ha sido encotrado";

        public const string IdError = "El id: {0}, no exite registrado {1}";
        public const string IdNotFound = "El id: {0}, no coincide en {1}";
        public const string ParamNull = "El parametro en {0}, no puede ser nulo";
        public const string NotFound = "El {0} no ha sido encotrado";

        public const string createSuccessMsg = "{0}, guardado satisfactoriamente.";
        public const string updateSuccessMsg = "{0}, modificado satisfactoriamente.";
        public const string deleteSuccessMsg = "{0}, eliminado satisfactoriamente.";
        public const string errorMsg = "{0}, no se completo el proceso correctamente. Completar los campos obligatorios.";
        public const string errorDeleteMsg = "No se puede cancelar la solicitud, ya que fue {0}.";
    }
}
