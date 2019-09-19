Configuration of the Identity related services can be found in the Areas/Identity/IdentityHostingStartup.cs file.


The generated UI requires MVC. To add MVC to your app:
2. Call app.UseRouting() at the top your Configure method, and UseEndpoints() after authentication:
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        endpoints.MapRazorPages();
    });

Apps that use ASP.NET Core Identity should also use HTTPS. To enable HTTPS see https://go.microsoft.com/fwlink/?linkid=848054.

