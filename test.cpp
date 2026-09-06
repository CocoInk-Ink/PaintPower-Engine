#include <iostream>
#include <string>
#include <vector>

class Dog {
public:
	int age;
	std::string breed;
	std::string name;

	void bark() {
		std::cout << "Bark!" << std::endl;
	}

	void info() {
		std::cout << "Hello, my name is " << name << "! " << "I am " << age << " years old! I am a " << breed << "!" << std::endl;
	}

	Dog(int age, std::string breed, std::string name) {
		this->age = age;
		this->breed = breed;
		this->name = name;
	}
};

Dog MakeDog(int age, std::string breed, std::string name) {
	Dog dog(age, breed, name);
	dog.bark();

	return dog;
}

void loopDogList(std::vector<Dog> doglist) {
	for (int i = 0; i < doglist.size(); i++) {
		doglist[i].info();
	}
}

int main() {
	// system("cls");

	int boop[30][30];

	Dog Daisy = MakeDog(2, "Golden Retriever", "Daisy");
	Dog George = MakeDog(13, "German Shepard", "George");

	std::vector<Dog> dogs;

	dogs.push_back(Daisy);
	dogs.push_back(George);
	dogs.push_back(MakeDog(4, "Poodle", "Minnie"));

	loopDogList(dogs);

	return 0;
}