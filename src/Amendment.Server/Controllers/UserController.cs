//using Amendment.Shared.Requests;
//using Amendment.Shared.Responses;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Http.HttpResults;
//using Microsoft.AspNetCore.Mvc;

//namespace Amendment.Server.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class UserController : ControllerBase
//    {
//        [HttpGet]
//        public Results<Ok<IEnumerable<UserResponse>>, NotFound> Get()
//        {
            
//        }

//        [HttpGet("{id}")]
//        public Results<Ok<UserResponse>, NotFound> Get(int id)
//        {

//        }

//        [HttpPost]
//        public Results<Created<UserResponse>, NotFound> Post([FromBody] UserRequest model)
//        {

//        }

//        [HttpPut("{id}")]
//        public Results<Created<UserResponse>, NotFound> Put(int id, [FromBody] UserRequest model)
//        {

//        }

//        [HttpDelete("{id}")]
//        public Results<Ok, NotFound> Delete(int id)
//        {

//        }
//    }
//}
