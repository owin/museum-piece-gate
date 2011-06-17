%%{

machine config_string;

identifier = alpha {1} (alpha | digit)*;
dotted_identifier = (identifier ".")* identifier;
assembly = (any+ -- (".exe"i | ".dll"i)) (".exe"i | ".dll"i);

config_string = (dotted_identifier? >clear $buf %on_identifier) ((space* "," space*) (assembly >clear $buf %on_assembly))?;

main := config_string;

}%%