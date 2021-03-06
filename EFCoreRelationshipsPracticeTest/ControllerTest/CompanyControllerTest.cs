using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using EFCoreRelationshipsPractice;
using EFCoreRelationshipsPractice.Dtos;
using EFCoreRelationshipsPractice.Entities;
using EFCoreRelationshipsPractice.Repository;
using EFCoreRelationshipsPractice.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace EFCoreRelationshipsPracticeTest
{
    [Collection("IntegrationTest")]
    public class CompanyControllerTest : TestBase
    {
        private readonly HttpClient client;
        private readonly CompanyDto companyDto = new CompanyDto();
        private readonly string httpContent;
        private readonly StringContent content;
        private readonly CompanyDbContext context;
        private readonly CompanyService companyService;
        public CompanyControllerTest(CustomWebApplicationFactory<Startup> factory) : base(factory)
        {
            client = GetClient();
            companyDto.Name = "IBM";
            companyDto.Employees = new List<EmployeeDto>()
            {
                new EmployeeDto()
                {
                    Name = "Tom",
                    Age = 19
                },
                new EmployeeDto()
                {
                    Name = "Jim",
                    Age = 21
                }
            };

            companyDto.Profile = new ProfileDto()
            {
                RegisteredCapital = 100010,
                CertId = "100",
            };
            httpContent = JsonConvert.SerializeObject(companyDto);
            content = new StringContent(httpContent, Encoding.UTF8, MediaTypeNames.Application.Json);
            context = Factory.Services.CreateScope().ServiceProvider.GetRequiredService<CompanyDbContext>();
            companyService = new CompanyService(context);
        }

        [Fact]
        public async Task Should_create_company_employee_profile_success()
        {
            await client.PostAsync("/companies", content);

            var allCompaniesResponse = await client.GetAsync("/companies");
            var body = await allCompaniesResponse.Content.ReadAsStringAsync();

            var returnCompanies = JsonConvert.DeserializeObject<List<CompanyDto>>(body);

            Assert.Equal(1, returnCompanies.Count);
            Assert.Equal(2, returnCompanies[0].Employees.Count);

            var orderedReturnEmployees = returnCompanies[0].Employees.OrderBy(e => e.Age).ToList();
            var orderedOriginalEmployees = companyDto.Employees.OrderBy(e => e.Age).ToList();

            Assert.Equal(orderedReturnEmployees[0].Age, orderedOriginalEmployees[0].Age);
            Assert.Equal(orderedReturnEmployees[0].Name, orderedOriginalEmployees[0].Name);
            Assert.Equal(companyDto.Profile.CertId, returnCompanies[0].Profile.CertId);
            Assert.Equal(companyDto.Profile.RegisteredCapital, returnCompanies[0].Profile.RegisteredCapital);

            Assert.Equal(1, context.Companies.ToList().Count());
            var firstCompany = await context.Companies.Include(company => company.Profile).FirstOrDefaultAsync();
            Assert.Equal(companyDto.Profile.CertId, firstCompany.Profile.CertId);
        }

        [Fact]
        public async Task Should_delete_company_and_related_employee_and_profile_success()
        {
            var response = await client.PostAsync("/companies", content);
            await client.DeleteAsync(response.Headers.Location);
            var allCompaniesResponse = await client.GetAsync("/companies");
            var body = await allCompaniesResponse.Content.ReadAsStringAsync();

            var returnCompanies = JsonConvert.DeserializeObject<List<CompanyDto>>(body);

            Assert.Equal(0, returnCompanies.Count);
        }

        [Fact]
        public async Task Should_create_many_companies_success()
        {
            await client.PostAsync("/companies", content);
            await client.PostAsync("/companies", content);

            var allCompaniesResponse = await client.GetAsync("/companies");
            var body = await allCompaniesResponse.Content.ReadAsStringAsync();

            var returnCompanies = JsonConvert.DeserializeObject<List<CompanyDto>>(body);

            Assert.Equal(2, returnCompanies.Count);
        }

        [Fact]
        public async Task Should_create_company_success_via_company_service()
        {
            var scope = Factory.Services.CreateScope();
            var scopedServices = scope.ServiceProvider;

            CompanyDbContext context = scopedServices.GetRequiredService<CompanyDbContext>();

            CompanyService companyService = new CompanyService(context);

            await companyService.AddCompany(companyDto);

            Assert.Equal(1, context.Companies.Count());
        }

        [Fact]
        public async Task Should_get_allcompanies_success_via_company_service()
        {
            await companyService.AddCompany(companyDto);

            List<CompanyDto> companyDtos = await companyService.GetAll();

            Assert.Equal(1, context.Companies.Count());
            Assert.Equal(companyDto.Employees.Count, companyDtos[0].Employees.Count);
            Assert.Equal(companyDto.Employees[0].Age, companyDtos[0].Employees[0].Age);
            Assert.Equal(companyDto.Employees[0].Name, companyDtos[0].Employees[0].Name);
            Assert.Equal(companyDto.Profile.CertId, companyDtos[0].Profile.CertId);
            Assert.Equal(companyDto.Profile.RegisteredCapital, companyDtos[0].Profile.RegisteredCapital);
        }

        [Fact]
        public async Task Should_delete_company_success_via_company_service()
        {
            var id1 = await companyService.AddCompany(companyDto);
            var id2 = await companyService.AddCompany(companyDto);

            await companyService.DeleteCompany(id1);

            Assert.Equal(1, context.Companies.Count());
            Assert.True(context.Companies.Any(x => x.Id == id2));
        }

        [Fact]
        public async Task Should_get_company_byId_via_company_service()
        {
            var id = await companyService.AddCompany(companyDto);
            var returnCompanyDto = await companyService.GetById(id);

            Assert.Equal(companyDto.Employees.Count, returnCompanyDto.Employees.Count);
            Assert.Equal(companyDto.Employees[0].Age, returnCompanyDto.Employees[0].Age);
            Assert.Equal(companyDto.Employees[0].Name, returnCompanyDto.Employees[0].Name);
            Assert.Equal(companyDto.Profile.CertId, returnCompanyDto.Profile.CertId);
            Assert.Equal(companyDto.Profile.RegisteredCapital, returnCompanyDto.Profile.RegisteredCapital);
        }

        //[Fact]
        //public async Task Should_delete_company_success_via_company_service1()
        //{
        //    var id = await companyService.AddCompany(companyDto);

        //    await companyService.DeleteCompany(id);

        //    Assert.Equal(0, context.Companies.Count());
        //    Assert.Equal(0, context.Employees.Count());
        //}
    }
}