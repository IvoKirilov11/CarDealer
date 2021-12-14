using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoMapper;
using CarDealer.Data;
using CarDealer.DTO;
using CarDealer.Models;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;

namespace CarDealer
{
    public class StartUp
    {
        public static void Main(string[] args)
        {
            var context = new CarDealerContext();
            //context.Database.EnsureDeleted();
            //context.Database.EnsureCreated();

           // var json = File.ReadAllText("../../../Datasets/suppliers.json");
           // var partsJson = File.ReadAllText("../../../Datasets/parts.json");
           // var carsJson = File.ReadAllText("../../../Datasets/cars.json");
           // var customerJson = File.ReadAllText("../../../Datasets/customers.json");
           // var salesJson = File.ReadAllText("../../../Datasets/sales.json");
           // ImportSuppliers(context, json);
           //  ImportParts(context, partsJson);
           // ImportCars(context, carsJson);
           // ImportCustomers(context, customerJson);
           // ImportSales(context, salesJson);



            Console.WriteLine(GetSalesWithAppliedDiscount(context));
        }

        public static string ImportSuppliers(CarDealerContext context, string inputJson)
        {
            var supplyerDTO = JsonConvert.DeserializeObject<IEnumerable<InportSupplierInputModel>>(inputJson);

            var supplyers = supplyerDTO.Select(x => new Supplier
            {
                Name = x.Name,
                IsImporter = x.IsImporter
            })
                .ToList();

            context.Suppliers.AddRange(supplyers);
            context.SaveChanges();

            return $"Successfully imported {supplyers.Count}.";
        }
        public static string ImportParts(CarDealerContext context, string inputJson)
        {

            var supplyedId = context.Suppliers.Select(x => x.Id).ToArray();
           
            var parts = JsonConvert.DeserializeObject<IEnumerable<Part>>(inputJson)
                .Where(x => supplyedId.Contains(x.SupplierId)).ToList();

            
            context.Parts.AddRange(parts);
            context.SaveChanges();

            return $"Successfully imported {parts.Count()}.";

        }
        public static string ImportCars(CarDealerContext context, string inputJson)
        {
            var carsDTO = JsonConvert.DeserializeObject<IEnumerable<CarInputModel>>(inputJson);

            var listOfCars = new List<Car>();

            foreach (var car in carsDTO)
            {
                var currentCars = new Car
                {
                    Make = car.Make,
                    Model = car.Model,
                    TravelledDistance = car.TravelledDistance


                };
                foreach (var partId in car?.PartsId.Distinct())
                {
                    currentCars.PartCars.Add(new PartCar
                    {
                        PartId = partId
                    });

                }
               listOfCars.Add(currentCars);     
            }
                

            context.Cars.AddRange(listOfCars);
            context.SaveChanges();

            return $"Successfully imported {listOfCars.Count}.";
        }

        public static string ImportCustomers(CarDealerContext context, string inputJson)
        {
            var custumers = JsonConvert.DeserializeObject<IEnumerable<Customer>>(inputJson);

            context.AddRange(custumers);
            context.SaveChanges();

            return $"Successfully imported {custumers.Count()}.";
        }
        public static string ImportSales(CarDealerContext context, string inputJson)
        {
            var sales = JsonConvert.DeserializeObject<IEnumerable<Sale>>(inputJson);

            context.AddRange(sales);
            context.SaveChanges();

            return $"Successfully imported {sales.Count()}.";
        }
        public static string GetOrderedCustomers(CarDealerContext context)
        {
            var customers = context.Customers
                .Select(x => new
                {
                    Name = x.Name,
                    BirthDate = x.BirthDate,
                    IsYoungDriver = x.IsYoungDriver
                })
                .OrderBy(x => x.BirthDate)
                .ThenBy(x => x.IsYoungDriver)
                .ToArray();

            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.DateFormatString = "dd/MM/yyyy";

            var result = JsonConvert.SerializeObject(customers, Formatting.Indented, jsonSettings);

            return result;
        }

        public static string GetCarsFromMakeToyota(CarDealerContext context)
        {
            var cars = context.Cars
                .Where(x => x.Make == "Toyota")
                .Select(x => new
                {
                    Id = x.Id,
                    Make = x.Make,
                    Model = x.Model,
                    TravelledDistance = x.TravelledDistance

                })
                .OrderBy(x => x.Model)
                .ThenByDescending(x => x.TravelledDistance)
                .ToArray();

            var result = JsonConvert.SerializeObject(cars, Formatting.Indented);

            return result;
        }

        public static string GetLocalSuppliers(CarDealerContext context)
        {
            var suppliers = context.Suppliers
                .Where(x => x.IsImporter == false)
                .Select(x => new
                {
                    Id = x.Id,
                    Name = x.Name,
                    PartsCount = x.Parts.Count
                })
                .ToList();
            var result = JsonConvert.SerializeObject(suppliers, Formatting.Indented);

            return result;
        }

        public static string GetCarsWithTheirListOfParts(CarDealerContext context)
        {
            var cars = context.Cars
                .Select(x => new
                {
                    car = new
                    {
                        x.Make,
                        x.Model,
                        x.TravelledDistance,
                    },
                    parts = x.PartCars
                        .Where(p => p.CarId == x.Id)
                        .Select(p => new
                        {
                            p.Part.Name,
                            Price = p.Part.Price.ToString("F2")
                        }).ToList()
                })      
                .ToList();
            var result = JsonConvert.SerializeObject(cars, Formatting.Indented);

            return result;
        }
        public static string GetTotalSalesByCustomer(CarDealerContext context)
        {
            var customer = context.Customers
                .Include(x => x.Sales)
                .ThenInclude(x=>x.Car)
                .Where(x => x.Sales != null)
                .Select(x => new
                {
                    fullName = x.Name,
                    boughtCars = x.Sales.Count,
                    spentMoney = x.Sales.Select(s => s.Car).SelectMany(s => s.PartCars).Sum(s=> s.Part.Price)


                })
                .OrderByDescending(x => x.spentMoney)
                .ThenByDescending(x => x.boughtCars)
                .ToArray();

            var result = JsonConvert.SerializeObject(customer, Formatting.Indented);

            return result;

        }
        public static string GetSalesWithAppliedDiscount(CarDealerContext context)
        {
            var sales = context
                .Sales
                .Include(x => x.Car)
                .Select(x => new
                {
                    car = new
                    {
                        x.Car.Make,
                        x.Car.Model,
                        x.Car.TravelledDistance
                    },
                    customerName = x.Customer.Name,
                    Discount = x.Discount.ToString("F2"),
                    price = x.Car.PartCars.Sum(c => c.Part.Price).ToString("F2"),
                    priceWithDiscount = (x.Car.PartCars.Sum(c => c.Part.Price) - (x.Car.PartCars.Sum(c => c.Part.Price) * (x.Discount) / 100.0m)).ToString("F2"),
                })
                .Take(10)
                .ToList();

            var jsonOutput = JsonConvert.SerializeObject(sales, Formatting.Indented);
            return jsonOutput;
        }
    }
}