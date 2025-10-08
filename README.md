# User Management Technical Exercise

The exercise is an ASP.NET Core web application backed by Entity Framework Core, which facilitates management of some fictional users.
We recommend that you use [Visual Studio (Community Edition)](https://visualstudio.microsoft.com/downloads) or [Visual Studio Code](https://code.visualstudio.com/Download) to run and modify the application.

**The application uses an in-memory database, so changes will not be persisted between executions.**

## The Exercise
Complete as many of the tasks below as you feel comfortable with. These are split into 4 levels of difficulty
* **Standard** - Functionality that is common when working as a web developer
* **Advanced** - Slightly more technical tasks and problem solving
* **Expert** - Tasks with a higher level of problem solving and architecture needed
* **Platform** - Tasks with a focus on infrastructure and scaleability, rather than application development.

### 1. Filters Section (Standard)

The users page contains 3 buttons below the user listing - **Show All**, **Active Only** and **Non Active**. Show All has already been implemented. Implement the remaining buttons using the following logic:
* Active Only – This should show only users where their `IsActive` property is set to `true`
* Non Active – This should show only users where their `IsActive` property is set to `false`

### 2. User Model Properties (Standard)

Add a new property to the `User` class in the system called `DateOfBirth` which is to be used and displayed in relevant sections of the app.

### 3. Actions Section (Standard)

Create the code and UI flows for the following actions
* **Add** – A screen that allows you to create a new user and return to the list
* **View** - A screen that displays the information about a user
* **Edit** – A screen that allows you to edit a selected user from the list
* **Delete** – A screen that allows you to delete a selected user from the list

Each of these screens should contain appropriate data validation, which is communicated to the end user.

### 4. Data Logging (Advanced)

Extend the system to capture log information regarding primary actions performed on each user in the app.
* In the **View** screen there should be a list of all actions that have been performed against that user.
* There should be a new **Logs** page, containing a list of log entries across the application.
* In the Logs page, the user should be able to click into each entry to see more detail about it.
* In the Logs page, think about how you can provide a good user experience - even when there are many log entries.

### 5. Extend the Application (Expert)

Make a significant architectural change that improves the application.
Structurally, the user management application is very simple, and there are many ways it can be made more maintainable, scalable or testable.
Some ideas are:
* Re-implement the UI using a client side framework connecting to an API. Use of Blazor is preferred, but if you are more familiar with other frameworks, feel free to use them.
* Update the data access layer to support asynchronous operations.
* Implement authentication and login based on the users being stored.
* Implement bundling of static assets.
* Update the data access layer to use a real database, and implement database schema migrations.

### 6. Future-Proof the Application (Platform)

Add additional layers to the application that will ensure that it is scaleable with many users or developers. For example:
* Add CI pipelines to run tests and build the application.
* Add CD pipelines to deploy the application to cloud infrastructure.
* Add IaC to support easy deployment to new environments.
* Introduce a message bus and/or worker to handle long-running operations.

## Additional Notes

* Please feel free to change or refactor any code that has been supplied within the solution and think about clean maintainable code and architecture when extending the project.
* If any additional packages, tools or setup are required to run your completed version, please document these thoroughly.

---
## George's Notes and Documentation

Ultimately, I only managed to complete four of the six tasks in their entirety (#1, #2, #3, & #4). However, I did attempt the asynchronous operations task from section five and the CI/CD pipelines from section 6.

### Additional Packages Used

- AutoMapper 15.0.1
  - https://www.nuget.org/packages/AutoMapper
  - Used to make mapping between data entity objects and view models easy and tidy
- FluentValidation 12.0.0
  - https://www.nuget.org/packages/FluentValidation
  - Used to make writing validation classes (i.e. `UserInputViewModelValidator`) easy, with very readable code
- FluentValidation.DependencyInjectionExtensions 12.0.0
  - https://www.nuget.org/packages/FluentValidation.DependencyInjectionExtensions
  - Dependency injection extensions for the aforementioned FluentValidation package

### CI/CD Pipelines
The two workflow scripts can be found from the root of the repo in `.github/workflows`

#### Continuous Integration
- https://github.com/KartWrightfield/TechTest.Inflo/actions/workflows/ci.yml
- The CI pipeline is triggered by a pull request or push to main
- The pipeline builds the project, runs the tests and generates a test coverage report
- The report can then be viewed either in codecov (POC only) or on GitHub itself (e.g. https://github.com/KartWrightfield/TechTest.Inflo/actions/runs/18338847795/job/52229197640)

#### Continuous Deployment
- https://github.com/KartWrightfield/TechTest.Inflo/actions/workflows/cd.yml
- The CD pipeline is triggered by a successful execution of the CI pipeline
- The pipeline builds the project (this could be improved later by having it re-use the same build executed in the CI stage), simulates deployment and then generates a deployment report

### Final Notes and Observations

- I focused my efforts on what felt like the foundational steps of the test, rather than try and attempt literally everything, I hope that wasn't a mistake!
- Once those foundations were done, I picked the things from the fifth and sixth tasks which seemed most interesting to me (I've never written GitHub Actions before, but I'm glad to have had an excuse to do so!)
- With the deadline approaching, these are the next improvements that I would be prioritising if I had more time:
  1. Test coverage is focused more on the core classes (controllers/services). With the help of the CI coverage report, a more complete coverage can definitely be achieved
  2. Validation on the Create/Edit forms is currently very rudimentary, it could definitely be improved with things like limiting input string length, or preventing birth dates older than 'X' years, etc.
  3. The audit logging for user updates can be improved
     - The log entry becomes hard to read if multiple properties are changed, as it's just a long string of 'X has changed from Y to Z' statements
     - The comparison logic could be refactored into some kind of comparison helper or service that would be capable of logging changes even if the User class was extended to include more properties
