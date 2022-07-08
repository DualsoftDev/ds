type ContactMethod =
    | EmailAddress of address:string
    | Telephone of phone:int
    | Postal of {|Line1:string; Line2:string; PostCode:string|}

type Customer = {
    Name:string
    Age:int
    Contact:ContactMethod
}

let customer1 = {Name="john"; Age=18; Contact=Postal{|Line1="line1"; Line2="Line2"; PostCode="123"|} }

