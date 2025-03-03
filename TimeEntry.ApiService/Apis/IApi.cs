﻿namespace TimeEntry.ApiService.Apis
{
    /// <summary> An interface for Identifying and registering APIs </summary>
    public interface IApi
    {
        /// <summary> This is automatically called by the library to add your APIs </summary>
        /// <param name="builder">The Endpoint Route Builder object to register the API </param>
        void Register(WebApplication builder);
    }
}
